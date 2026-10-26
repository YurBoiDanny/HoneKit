namespace HoneKit.Requirements.Domain;

/// <summary>What the reader is and whether it can work, in one plain sentence.</summary>
public sealed record AiStatus(bool KeyConfigured, string Mode, string Detail);

/// <summary>
/// The reader's status rules, over plain configuration values. Nothing here touches the network or
/// the key itself; the endpoint adds the last-parse record and the optional live probe.
/// </summary>
public static class AiStatusRules
{
    public const string Fixture = "fixture";

    public const string Live = "live";

    public const string Unavailable = "unavailable";

    public static AiStatus Evaluate(string? provider, string? model, string? apiKey)
    {
        var normalized = (provider ?? "").Trim().ToLowerInvariant();
        var modelName = string.IsNullOrWhiteSpace(model) ? "the configured model" : model.Trim();
        var hasKey = !string.IsNullOrWhiteSpace(apiKey);

        return normalized switch
        {
            "fixture" => new AiStatus(
                KeyConfigured: true,
                Fixture,
                "Fixture reader ready; no network needed."),

            "broken" => new AiStatus(
                KeyConfigured: hasKey,
                Unavailable,
                "The reader is switched off (AI__PROVIDER=broken). Type the requirements instead."),

            // Ollama ignores the key, so a missing one does not stand in the way.
            "ollama" => new AiStatus(
                KeyConfigured: true,
                Live,
                $"Live Ollama reader configured for {modelName}; the key is ignored."),

            "openai" or "opencode" or "opencode-go" => hasKey
                ? new AiStatus(
                    KeyConfigured: true,
                    Live,
                    $"Live OpenAI-compatible reader configured for {modelName}.")
                : new AiStatus(
                    KeyConfigured: false,
                    Unavailable,
                    $"Live OpenAI-compatible reader configured for {modelName}, but no key is set."),

            _ => new AiStatus(
                KeyConfigured: hasKey,
                Unavailable,
                $"AI__PROVIDER '{normalized}' is not recognised.")
        };
    }

    /// <summary>Only a configured live reader is worth a network round trip.</summary>
    public static bool CanProbe(string mode) => mode == Live;
}
