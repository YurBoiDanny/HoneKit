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

        // The bag wall: every member of the role with the tools their completions earn. Skills and
        // lessons are fetched once for the role, then each member's completions are joined locally.
        api.MapGet("/teams/{role}/members", async Task<Results<Ok<BagWallDto>, ProblemHttpResult>> (
            string role,
            RosterDbContext db,
            ProgressClient progressClient,
            ContentClient contentClient,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var normalizedRole = role.Trim().ToLowerInvariant();

            var members = await db.Members.AsNoTracking()
                .Where(m => m.PrimaryRole == normalizedRole || m.SecondaryRole == normalizedRole)
                .OrderBy(m => m.Name)
                .ToListAsync(ct);

            var logger = loggerFactory.CreateLogger("Roster.BagWall");

            SkillDto[] skills;
            LessonDto[] lessons;
            try
            {
                skills = await contentClient.GetSkillsAsync(normalizedRole, ct);
                lessons = await contentClient.GetLessonsAsync(normalizedRole, ct);
            }
            catch (Exception exception)
            {
                return Unavailable("Content", exception, logger);
            }

            var distinctSkills = skills.DistinctBy(skill => skill.Id).ToArray();
            var distinctLessons = lessons.DistinctBy(lesson => lesson.Id).ToArray();

            // DB access stays sequential (one context per request) and the Progress calls are made
            // one member at a time: six small reads, and a failure names Progress cleanly.
            var wall = new List<BagWallMemberDto>(members.Count);

            foreach (var member in members)
            {
                CompletionsDto completions;
                try
                {
                    completions = await progressClient.GetCompletionsAsync(member.Id, ct);
                }
                catch (Exception exception)
                {
                    return Unavailable("Progress", exception, logger);
                }

                var earnedSkillIds = BagRules.EarnedSkillIds(completions.LessonIds, distinctLessons, distinctSkills);

                // Same mapping as the member bag, so the wall shows the tools in the same order.
                var toolNames = BagRules.Tools(earnedSkillIds, distinctSkills, [], [])
                    .Select(tool => tool.Name)
                    .ToArray();

                wall.Add(new BagWallMemberDto(
                    member.Id,
                    member.Name,
                    member.Colour,
                    member.Style,
                    member.Worn,
                    toolNames,
                    member.LastActiveAt));
            }

            return TypedResults.Ok(new BagWallDto(normalizedRole, TeamName(normalizedRole), wall));
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

            // One extra Progress call, and only worth making when there is a tool to decorate.
            IReadOnlyList<string> honedSkillIds = [];
            if (earnedSkillIds.Count > 0)
            {
                try
                {
                    honedSkillIds = (await progressClient.GetHonedSkillsAsync(member.Id, ct)).SkillIds;
                }
                catch (Exception exception)
                {
                    return Unavailable("Progress", exception, logger);
                }
            }

            var acknowledged = (await db.BagAcknowledgements.AsNoTracking()
                    .Where(a => a.MemberId == member.Id)
                    .Select(a => a.SkillId)
                    .ToListAsync(ct))
                .ToHashSet(StringComparer.Ordinal);

            var tools = BagRules.Tools(earnedSkillIds, distinctSkills, honedSkillIds, acknowledged);

            return TypedResults.Ok(new BagDto(
                member.Id,
                member.Name,
                member.Colour,
                member.Style,
                member.Worn,
                tools,
                member.ShowAnswerOnWrong));
        });

        // No auth in the POC: the frontend only offers this for the demo member. The stored tokens
        // are the closed vocabulary the UI draws from, so anything else is a bad request.
        api.MapPatch("/members/{id}/bag", async Task<Results<NoContent, NotFound, BadRequest<string>>> (
            string id, BagStyleRequest request, RosterDbContext db, CancellationToken ct) =>
        {
            var member = await db.Members.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (member is null)
            {
                return TypedResults.NotFound();
            }

            if (!BagStyleRules.TryValidate(request.Colour, request.Style, out var error))
            {
                return TypedResults.BadRequest(error);
            }

            member.Colour = request.Colour;
            member.Style = request.Style;
            await db.SaveChangesAsync(ct);

            return TypedResults.NoContent();
        });

        // Marks the drop as seen so the animation fires once. Idempotent: the client may retry.
        // The skill is not checked against earned tools; the client only acknowledges what it saw.
        api.MapPost("/members/{id}/bag/acknowledge", async Task<Results<NoContent, NotFound>> (
            string id, AcknowledgeRequest request, RosterDbContext db, CancellationToken ct) =>
        {
            var memberExists = await db.Members.AsNoTracking().AnyAsync(m => m.Id == id, ct);
            if (!memberExists)
            {
                return TypedResults.NotFound();
            }

            var alreadyAcknowledged = await db.BagAcknowledgements
                .AnyAsync(a => a.MemberId == id && a.SkillId == request.SkillId, ct);

            if (!alreadyAcknowledged)
            {
                db.BagAcknowledgements.Add(new BagAcknowledgement
                {
                    MemberId = id,
                    SkillId = request.SkillId,
                    AcknowledgedAt = DateTimeOffset.UtcNow
                });

                await db.SaveChangesAsync(ct);
            }

            return TypedResults.NoContent();
        });

        // No auth in the POC: the demo member edits their own preference, but nothing is secret.
        api.MapPatch("/members/{id}/preferences", async Task<Results<NoContent, NotFound>> (
            string id, PreferencesRequest request, RosterDbContext db, CancellationToken ct) =>
        {
            var member = await db.Members.FirstOrDefaultAsync(m => m.Id == id, ct);
            if (member is null)
            {
                return TypedResults.NotFound();
            }

            member.ShowAnswerOnWrong = request.ShowAnswerOnWrong;
            await db.SaveChangesAsync(ct);

            return TypedResults.NoContent();
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
            detail: $"The bags could not be built because the {service} service did not respond.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?> { ["service"] = service.ToLowerInvariant() });
    }
}
