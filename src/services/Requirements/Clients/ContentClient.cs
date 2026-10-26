using System.Net.Http.Json;
using HoneKit.Contracts;

namespace HoneKit.Requirements.Clients;

/// <summary>The closed vocabulary: the only skills a requirement may name.</summary>
public sealed class ContentClient(HttpClient http)
{
    public async Task<SkillDto[]> GetSkillsAsync(string role, CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<SkillDto[]>(
            $"/api/roles/{Uri.EscapeDataString(role)}/skills",
            cancellationToken) ?? [];
}
