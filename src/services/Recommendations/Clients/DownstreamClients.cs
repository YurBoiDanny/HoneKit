using System.Net;
using System.Net.Http.Json;
using HoneKit.Contracts;

namespace HoneKit.Recommendations.Clients;

/// <summary>The confirmed requirement set. Recommendations only reads it.</summary>
public sealed class RequirementsClient(HttpClient http)
{
    public async Task<RequirementSetDto?> GetAsync(string requirementId, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(
            $"/api/requirements/{Uri.EscapeDataString(requirementId)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<RequirementSetDto>(cancellationToken)
            ?? throw new InvalidOperationException("Requirements returned an empty requirement set.");
    }
}

public sealed class RosterClient(HttpClient http)
{
    public async Task<TeamDto> GetTeamAsync(string role, CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<TeamDto>(
            $"/api/teams/{Uri.EscapeDataString(role)}",
            cancellationToken) ?? throw new InvalidOperationException("Roster returned an empty team.");
}

public sealed class ProgressClient(HttpClient http)
{
    public async Task<MemberProgressDto> GetProgressAsync(string memberId, CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<MemberProgressDto>(
            $"/api/members/{Uri.EscapeDataString(memberId)}/progress",
            cancellationToken) ?? throw new InvalidOperationException("Progress returned empty progress.");
}

public sealed class ContentClient(HttpClient http)
{
    public async Task<DeckSummaryDto[]> GetDecksAsync(string role, CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<DeckSummaryDto[]>(
            $"/api/decks?role={Uri.EscapeDataString(role)}",
            cancellationToken) ?? [];
}
