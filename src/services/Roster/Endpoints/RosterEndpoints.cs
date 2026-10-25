using System.Globalization;
using HoneKit.Contracts;
using HoneKit.Roster.Clients;
using HoneKit.Roster.Data;
using HoneKit.Roster.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HoneKit.Roster.Endpoints;

public static class RosterEndpoints
{
    public static IEndpointRouteBuilder MapRosterEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/teams/{role}", async Task<Ok<TeamDto>> (string role, RosterDbContext db, CancellationToken ct) =>
        {
            var normalizedRole = role.Trim().ToLowerInvariant();

            var members = await db.Members.AsNoTracking()
                .Where(m => m.PrimaryRole == normalizedRole || m.SecondaryRole == normalizedRole)
                .OrderBy(m => m.Name)
                .Select(m => new MemberDto(
                    m.Id,
                    m.Name,
                    m.PrimaryRole,
                    m.SecondaryRole,
                    m.Colour,
                    m.Style,
                    m.Worn,
                    m.LastActiveAt))
                .ToArrayAsync(ct);

            return TypedResults.Ok(new TeamDto(normalizedRole, TeamName(normalizedRole), members));
        });

        api.MapGet("/members/{id}/bag", async Task<Results<Ok<BagDto>, NotFound, ProblemHttpResult>> (
            string id,
            RosterDbContext db,
            ProgressClient progressClient,
            ContentClient contentClient,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var member = await db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct);
            if (member is null)
            {
                return TypedResults.NotFound();
            }

            var logger = loggerFactory.CreateLogger("Roster.Bag");

            CompletionsDto completions;
            try
            {
                completions = await progressClient.GetCompletionsAsync(member.Id, ct);
            }
            catch (Exception exception)
            {
                return Unavailable("Progress", exception, logger);
            }

            var roles = new[] { member.PrimaryRole, member.SecondaryRole }
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role!)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            var skills = new List<SkillDto>();
            var lessons = new List<LessonDto>();

            foreach (var role in roles)
            {
                try
                {
                    skills.AddRange(await contentClient.GetSkillsAsync(role, ct));
                    lessons.AddRange(await contentClient.GetLessonsAsync(role, ct));
                }
                catch (Exception exception)
                {
                    return Unavailable("Content", exception, logger);
                }
            }

            var distinctSkills = skills
                .DistinctBy(skill => skill.Id)
                .OrderBy(skill => skill.Name)
                .ToArray();

            var earnedSkillIds = BagRules.EarnedSkillIds(
                completions.LessonIds,
                lessons.DistinctBy(lesson => lesson.Id),
                distinctSkills);

            var acknowledged = (await db.BagAcknowledgements.AsNoTracking()
                    .Where(a => a.MemberId == member.Id)
                    .Select(a => a.SkillId)
                    .ToListAsync(ct))
                .ToHashSet(StringComparer.Ordinal);

            var tools = earnedSkillIds
                .Select(skillId => distinctSkills.First(skill => skill.Id == skillId))
                .Select(skill => new EarnedToolDto(
                    skill.Id,
                    skill.Name,
                    Honed: false,
                    Acknowledged: acknowledged.Contains(skill.Id)))
                .ToArray();

            return TypedResults.Ok(new BagDto(
                member.Id,
                member.Name,
                member.Colour,
                member.Style,
                member.Worn,
                tools));
        });

        return app;
    }

    private static string TeamName(string role) =>
        role == "camera"
            ? "Camera"
            : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(role.Replace('-', ' '));

    private static ProblemHttpResult Unavailable(string service, Exception exception, ILogger logger)
    {
        logger.LogWarning(exception, "Could not build a bag: {Service} is unavailable", service);

        return TypedResults.Problem(
            title: $"{service} is unavailable",
            detail: $"The bag could not be built because the {service} service did not respond.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?> { ["service"] = service.ToLowerInvariant() });
    }
}
