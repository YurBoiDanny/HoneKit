using System.Net;
using System.Net.Http.Json;
using HoneKit.Contracts;

namespace HoneKit.Progress.Clients;

// The slice of Content that Progress grades against. Declared here rather than shared so Progress
// depends on the wire shape it actually uses, not on Content's endpoint classes.
public sealed record DeckPayload(
    string Id,
    string Role,
    string Name,
    string SkillId,
    IReadOnlyList<CardDto> Cards);

public sealed class ContentClient(HttpClient http)
{
    public const string FillInTheBlank = "fill-in-the-blank";

    public const string Framing = "framing";

    public const string Ordering = "ordering";

    public const string Ptz = "ptz";

    /// <summary>The deck, or null when Content does not know it.</summary>
    public async Task<DeckPayload?> GetDeckAsync(string deckId, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(
            $"/api/decks/{Uri.EscapeDataString(deckId)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DeckPayload>(cancellationToken)
            ?? throw new InvalidOperationException("Content returned an empty deck payload.");
    }

    /// <summary>The card itself, or null when Content does not know it.</summary>
    public async Task<CardDto?> GetCardAsync(string cardId, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(
            $"/api/cards/{Uri.EscapeDataString(cardId)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CardDto>(cancellationToken)
            ?? throw new InvalidOperationException("Content returned an empty card payload.");
    }

    /// <summary>The grading payload for one card, or null when Content does not know it.</summary>
    public async Task<CardAnswerDto?> GetCardAnswerAsync(string cardId, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(
            $"/api/cards/{Uri.EscapeDataString(cardId)}/answer",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<CardAnswerDto>(cancellationToken)
            ?? throw new InvalidOperationException("Content returned an empty card answer payload.");
    }

    public async Task<SkillDto[]> GetSkillsAsync(string role, CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<SkillDto[]>(
            $"/api/roles/{Uri.EscapeDataString(role)}/skills",
            cancellationToken) ?? [];

    public async Task<LessonDto[]> GetLessonsAsync(string role, CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<LessonDto[]>(
            $"/api/roles/{Uri.EscapeDataString(role)}/lessons",
            cancellationToken) ?? [];
}
