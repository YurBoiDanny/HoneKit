using System.ClientModel;
using System.Text.Json;
using HoneKit.Contracts;
using HoneKit.Requirements.Data;
using HoneKit.Requirements.Domain;
using Microsoft.Extensions.AI;
using OpenAI;

namespace HoneKit.Requirements.Parsing;

/// <summary>
/// Turns brief text into a validated draft. The fixture provider replays the committed parse and is
/// the default; the live providers share one OpenAI-compatible client and one retry path. Only the
/// extracted text and the skill catalogue leave the process, and the text is never logged.
/// </summary>
public sealed class RequirementParser(IConfiguration configuration, ILogger<RequirementParser> logger)
{
    public const int TimeoutSeconds = 30;

    private const string UnavailableDetail = "Reading is unavailable right now — type the requirements instead.";

    private readonly object statusLock = new();

    /// <summary>The last parse attempt, held in memory: a restart clears it.</summary>
    public DateTimeOffset? LastParseAt { get; private set; }

    public bool? LastParseOk { get; private set; }

    public async Task<ParseResult> ParseAsync(
        string sourceName,
        string sourceText,
        IReadOnlyList<SkillDto> catalogue,
        CancellationToken cancellationToken)
    {
        var result = await ParseCoreAsync(sourceName, sourceText, catalogue, cancellationToken);

        lock (statusLock)
        {
            LastParseAt = DateTimeOffset.UtcNow;
            LastParseOk = result.Succeeded;
        }

        return result;
    }

    /// <summary>One tiny live call, used by the status strip's Check button.</summary>
    public async Task<(bool Ok, string Reply)> ProbeAsync(CancellationToken cancellationToken)
    {
        var settings = AiSettings.From(configuration);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

        try
        {
            var client = CreateClient(settings.Model, settings.Endpoint, settings.ApiKey);

            var response = await client.GetResponseAsync(
                "Reply with the single word ready.",
                new ChatOptions { Temperature = 0 },
                timeout.Token);

            return (true, (response.Text ?? "").Trim());
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The live reader probe failed");
            return (false, "");
        }
    }

    private async Task<ParseResult> ParseCoreAsync(
        string sourceName,
        string sourceText,
        IReadOnlyList<SkillDto> catalogue,
        CancellationToken cancellationToken)
    {
        var settings = AiSettings.From(configuration);

        return settings.Provider switch
        {
            "fixture" => RunFixture(sourceName, sourceText, catalogue),
            "broken" => ParseResult.Failed(
                503,
                UnavailableDetail,
                sourceName,
                sourceText,
                ["The AI provider is switched off (AI__PROVIDER=broken)."],
                settings.Provider,
                settings.Model),
            "openai" or "ollama" or "opencode" or "opencode-go" => await RunModelAsync(
                settings.Provider,
                settings.Model,
                settings.Endpoint,
                settings.ApiKey,
                sourceName,
                sourceText,
                catalogue,
                cancellationToken),
            _ => ParseResult.Failed(
                503,
                UnavailableDetail,
                sourceName,
                sourceText,
                [$"AI__PROVIDER '{settings.Provider}' is not recognised."],
                settings.Provider,
                settings.Model)
        };
    }

    /// <summary>
    /// The demo path. The fixture is still validated through the same rules, so a bad edit to the
    /// committed JSON fails visibly instead of producing a shapeless set.
    /// </summary>
    private static ParseResult RunFixture(string sourceName, string sourceText, IReadOnlyList<SkillDto> catalogue)
    {
        var draft = RequirementDraft.Normalize(FixtureParse.Load());
        var errors = RequirementValidation.Validate(draft, catalogue);

        return errors.Count == 0
            ? ParseResult.Success(draft, "fixture", "")
            : ParseResult.Failed(
                422,
                "The committed fixture parse no longer matches the skill catalogue.",
                sourceName,
                sourceText,
                errors,
                "fixture",
                "");
    }

    private async Task<ParseResult> RunModelAsync(
        string provider,
        string model,
        string endpoint,
        string apiKey,
        string sourceName,
        string sourceText,
        IReadOnlyList<SkillDto> catalogue,
        CancellationToken cancellationToken)
    {
        // Ollama ignores the key; the OpenAI SDK still wants a non-empty credential.
        if (provider != "ollama" && string.IsNullOrWhiteSpace(apiKey))
        {
            return ParseResult.Failed(
                503,
                "No model key is configured — type the requirements instead.",
                sourceName,
                sourceText,
                ["Set AI__APIKEY to read briefs with this provider."],
                provider,
                model);
        }

        IChatClient client;
        try
        {
            client = CreateClient(model, endpoint, apiKey);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The {Provider} client could not be created", provider);
            return ParseResult.Failed(
                503,
                UnavailableDetail,
                sourceName,
                sourceText,
                ["The model endpoint is not configured correctly."],
                provider,
                model);
        }

        // One budget covers both attempts: nobody waits longer than half a minute.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

        try
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, SystemPrompt(catalogue)),
                new(ChatRole.User, sourceText)
            };

            var first = RequirementDraft.Normalize(await AskAsync(client, messages, timeout.Token));
            var errors = RequirementValidation.Validate(first, catalogue);

            if (errors.Count == 0)
            {
                logger.LogInformation("Read a brief with {Provider}/{Model}", provider, model);
                return ParseResult.Success(first, provider, model);
            }

            // One retry, with the validator's own sentences attached.
            messages.Add(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(first, FixtureParse.JsonOptions)));
            messages.Add(new ChatMessage(ChatRole.User, RetryPrompt(errors)));

            var second = RequirementDraft.Normalize(await AskAsync(client, messages, timeout.Token));
            var secondErrors = RequirementValidation.Validate(second, catalogue);

            if (secondErrors.Count == 0)
            {
                logger.LogInformation("Read a brief with {Provider}/{Model} after one retry", provider, model);
                return ParseResult.Success(second, provider, model);
            }

            return ParseResult.Failed(
                422,
                "The model's answer did not match what HoneKit can plan.",
                sourceName,
                sourceText,
                secondErrors,
                provider,
                model);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("The {Provider} model did not answer within {Timeout}s", provider, TimeoutSeconds);
            return ParseResult.Failed(
                503,
                $"Reading took longer than {TimeoutSeconds} seconds — type the requirements instead.",
                sourceName,
                sourceText,
                ["The model did not answer in time."],
                provider,
                model);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The {Provider} model call failed", provider);
            return ParseResult.Failed(
                503,
                UnavailableDetail,
                sourceName,
                sourceText,
                ["The model could not be reached."],
                provider,
                model);
        }
    }

    private static IChatClient CreateClient(string model, string endpoint, string apiKey)
    {
        var credential = new ApiKeyCredential(string.IsNullOrWhiteSpace(apiKey) ? "ollama" : apiKey);
        var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };

        return new OpenAIClient(credential, options)
            .GetChatClient(model)
            .AsIChatClient();
    }

    private static async Task<ParsedRequirements> AskAsync(
        IChatClient client,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var response = await client.GetResponseAsync<ParsedRequirements>(
            messages,
            new ChatOptions { Temperature = 0 },
            useJsonSchemaResponseFormat: true,
            cancellationToken: cancellationToken);

        return response.Result
            ?? throw new InvalidOperationException("The model returned no structured result.");
    }

    private static string SystemPrompt(IReadOnlyList<SkillDto> catalogue)
    {
        var skills = string.Join(", ", catalogue
            .OrderBy(skill => skill.Name, StringComparer.Ordinal)
            .Select(skill => $"{skill.Id} ({skill.Name})"));

        return $"""
            You turn a meeting brief into camera-crew requirements for HoneKit.
            Reply with JSON only, matching the schema.
            purpose: one plain sentence describing the meeting.
            criticality: low, medium or high.
            skills: choose skillId only from this catalogue: {skills}. minLevel must be 0, 0.5 or 1.
            headcount must be between 1 and 20. rationale: one short sentence tying the skill to the meeting.
            outsideRole: services the brief asks for that are not camera skills, as plain names.
            Never invent a skillId for them.
            """;
    }

    private static string RetryPrompt(IReadOnlyList<string> errors) =>
        $"""
         That answer failed validation:
         {string.Join(Environment.NewLine, errors.Select(error => $"- {error}"))}
         Reply again with corrected JSON that follows every rule.
         """;
}
