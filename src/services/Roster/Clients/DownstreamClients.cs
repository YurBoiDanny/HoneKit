using System.Net.Http.Json;
using HoneKit.Contracts;

namespace HoneKit.Roster.Clients;

public sealed class ProgressClient(HttpClient http)
{
    public async Task<CompletionsDto> GetCompletionsAsync(string memberId, CancellationToken cancellationToken)
    {
        var completions = await http.GetFromJsonAsync<CompletionsDto>(
            $"/api/members/{Uri.EscapeDataString(memberId)}/completions",
            cancellationToken);

        return completions ?? throw new InvalidOperationException("Progress returned an empty completions payload.");
    }

    public async Task<HonedSkillsDto> GetHonedSkillsAsync(string memberId, CancellationToken cancellationToken)
    {
        var honed = await http.GetFromJsonAsync<HonedSkillsDto>(
            $"/api/members/{Uri.EscapeDataString(memberId)}/honed-skills",
            cancellationToken);

        return honed ?? throw new InvalidOperationException("Progress returned an empty honed-skills payload.");
    }
}

public sealed class ContentClient(HttpClient http)
{
    public async Task<SkillDto[]> GetSkillsAsync(string role, CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<SkillDto[]>(
            $"/api/roles/{Uri.EscapeDataString(role)}/skills",
            cancellationToken) ?? [];

    public async Task<LessonDto[]> GetLessonsAsync(string role, CancellationToken cancellationToken) =>
        await http.GetFromJsonAsync<LessonDto[]>(
            $"/api/roles/{Uri.EscapeDataString(role)}/lessons",
            cancellationToken) ?? [];
}
