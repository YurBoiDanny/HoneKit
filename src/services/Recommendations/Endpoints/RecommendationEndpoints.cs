using HoneKit.Contracts;
using HoneKit.Recommendations.Clients;
using HoneKit.Recommendations.Data;
using HoneKit.Recommendations.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HoneKit.Recommendations.Endpoints;

public static class RecommendationEndpoints
{
    // The POC has one role; plans are always for the camera crew.
    private const string Role = "camera";

    public static IEndpointRouteBuilder MapRecommendationEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/recommendations");
        var plans = app.MapGroup("/api/plans");

        api.MapPost("", RankAsync);

        api.MapPost("/check", CheckAsync);

        plans.MapPost("", SavePlanAsync);

        plans.MapGet("/{id}", async Task<Results<Ok<PlanDto>, NotFound>> (
            string id, RecommendationsDbContext db, CancellationToken ct) =>
        {
            var plan = await db.Plans.AsNoTracking()
                .Include(plan => plan.Members)
                .Include(plan => plan.Gaps)
                .FirstOrDefaultAsync(plan => plan.Id == id, ct);

            return plan is null ? TypedResults.NotFound() : TypedResults.Ok(ToDto(plan));
        });

        plans.MapGet("", async Task<Ok<PlanSummaryDto[]>> (RecommendationsDbContext db, CancellationToken ct) =>
        {
            // SQLite cannot ORDER BY a DateTimeOffset, and there are only a handful of plans.
            var saved = await db.Plans.AsNoTracking()
                .Include(plan => plan.Members)
                .ToListAsync(ct);

            var summaries = saved
                .OrderByDescending(plan => plan.CreatedAt)
                .ThenByDescending(plan => plan.Id, StringComparer.Ordinal)
                .Select(plan => new PlanSummaryDto(
                    plan.Id,
                    plan.RequirementId,
                    plan.Purpose,
                    plan.Strategy,
                    plan.CreatedAt,
                    plan.Members.Count))
                .ToArray();

            return TypedResults.Ok(summaries);
        });

        return app;
    }

    private static async Task<Results<Ok<RecommendationDto>, NotFound, BadRequest<string>, ProblemHttpResult>> RankAsync(
        RecommendationRequest request,
        RequirementsClient requirements,
        RosterClient roster,
        ProgressClient progressClient,
        ContentClient content,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        if (!RecommendationRules.IsStrategy(request.Strategy))
        {
            return UnknownStrategy();
        }

        var (set, setProblem) = await LoadSetAsync(request.RequirementId, requirements, loggerFactory, ct);
        if (setProblem is not null)
        {
            return setProblem;
        }

        if (set is null)
        {
            return TypedResults.NotFound();
        }

        if (set.ConfirmedAt is null)
        {
            return NotConfirmed();
        }

        var (team, teamProblem) = await LoadTeamAsync(roster, loggerFactory, ct);
        if (teamProblem is not null)
        {
            return teamProblem;
        }

        DeckSummaryDto[] decks;
        try
        {
            decks = await content.GetDecksAsync(Role, ct);
        }
        catch (Exception exception)
        {
            return Unavailable("Content", exception, loggerFactory);
        }

        var (progress, progressProblem) = await LoadProgressAsync(team!.Members, progressClient, loggerFactory, ct);
        if (progressProblem is not null)
        {
            return progressProblem;
        }

        var context = BuildContext(set, team.Members, progress!, decks, DateTimeOffset.UtcNow);
        var assessments = RecommendationRules.Assess(context);
        var suggestions = RecommendationRules.Suggest(context, request.Strategy);

        return TypedResults.Ok(new RecommendationDto(
            request.RequirementId,
            request.Strategy,
            RecommendationRules.Coverage(context, suggestions),
            suggestions.Select(ToDto).ToArray(),
            RecommendationRules.Gaps(context, suggestions, assessments),
            RecommendationRules.Sustain(context, assessments)));
    }

    private static async Task<Results<Ok<OverrideWarningDto[]>, NotFound, BadRequest<string>, ProblemHttpResult>> CheckAsync(
        OverrideCheckRequest request,
        RequirementsClient requirements,
        RosterClient roster,
        ProgressClient progressClient,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var (set, setProblem) = await LoadSetAsync(request.RequirementId, requirements, loggerFactory, ct);
        if (setProblem is not null)
        {
            return setProblem;
        }

        if (set is null)
        {
            return TypedResults.NotFound();
        }

        if (set.ConfirmedAt is null)
        {
            return NotConfirmed();
        }

        var (team, teamProblem) = await LoadTeamAsync(roster, loggerFactory, ct);
        if (teamProblem is not null)
        {
            return teamProblem;
        }

        var memberIds = (request.MemberIds ?? [])
            .Where(memberId => !string.IsNullOrWhiteSpace(memberId))
            .Select(memberId => memberId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var unknown = memberIds.FirstOrDefault(id => team!.Members.All(member => member.Id != id));
        if (unknown is not null)
        {
            return TypedResults.BadRequest($"'{unknown}' is not on the {Role} team.");
        }

        var selected = team!.Members.Where(member => memberIds.Contains(member.Id, StringComparer.Ordinal)).ToArray();
        var (progress, progressProblem) = await LoadProgressAsync(selected, progressClient, loggerFactory, ct);
        if (progressProblem is not null)
        {
            return progressProblem;
        }

        var context = BuildContext(set, selected, progress!, [], DateTimeOffset.UtcNow);
        var assessments = RecommendationRules.Assess(context)
            .ToDictionary(assessment => assessment.Member.MemberId, StringComparer.Ordinal);

        var warnings = memberIds
            .Select(id => new OverrideWarningDto(id, assessments[id].Member.Name, assessments[id].Warnings))
            .ToArray();

        return TypedResults.Ok(warnings);
    }

    private static async Task<Results<Created<PlanDto>, NotFound, BadRequest<string>, ProblemHttpResult>> SavePlanAsync(
        SavePlanRequest request,
        RecommendationsDbContext db,
        RequirementsClient requirements,
        RosterClient roster,
        ProgressClient progressClient,
        ContentClient content,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        if (!RecommendationRules.IsStrategy(request.Strategy))
        {
            return UnknownStrategy();
        }

        var (set, setProblem) = await LoadSetAsync(request.RequirementId, requirements, loggerFactory, ct);
        if (setProblem is not null)
        {
            return setProblem;
        }

        if (set is null)
        {
            return TypedResults.NotFound();
        }

        if (set.ConfirmedAt is null)
        {
            return NotConfirmed();
        }

        var (team, teamProblem) = await LoadTeamAsync(roster, loggerFactory, ct);
        if (teamProblem is not null)
        {
            return teamProblem;
        }

        var memberIds = (request.MemberIds ?? [])
            .Where(memberId => !string.IsNullOrWhiteSpace(memberId))
            .Select(memberId => memberId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var unknown = memberIds.FirstOrDefault(id => team!.Members.All(member => member.Id != id));
        if (unknown is not null)
        {
            return TypedResults.BadRequest($"'{unknown}' is not on the {Role} team.");
        }

        DeckSummaryDto[] decks;
        try
        {
            decks = await content.GetDecksAsync(Role, ct);
        }
        catch (Exception exception)
        {
            return Unavailable("Content", exception, loggerFactory);
        }

        // Gaps name every below-level member, so the plan needs the whole crew's progress.
        var (progress, progressProblem) = await LoadProgressAsync(team!.Members, progressClient, loggerFactory, ct);
        if (progressProblem is not null)
        {
            return progressProblem;
        }

        var context = BuildContext(set, team.Members, progress!, decks, DateTimeOffset.UtcNow);
        var assessments = RecommendationRules.Assess(context)
            .ToDictionary(assessment => assessment.Member.MemberId, StringComparer.Ordinal);

        var selected = memberIds.Select(id => assessments[id]).ToArray();
        var gaps = RecommendationRules.Gaps(context, selected, assessments.Values.ToArray());

        var planId = $"plan-{Guid.NewGuid():N}";

        var plan = new Plan
        {
            Id = planId,
            RequirementId = set.Id,
            Purpose = set.Purpose,
            Strategy = request.Strategy,
            CreatedAt = DateTimeOffset.UtcNow,
            Members = selected.Select((assessment, index) => new PlanMember
            {
                PlanId = planId,
                MemberId = assessment.Member.MemberId,
                Name = assessment.Member.Name,
                AtLevel = assessment.Warnings.Count == 0,
                WarningsJson = RecommendationsJson.Write(assessment.Warnings),
                Ordinal = index
            }).ToList(),
            Gaps = gaps.Select((gap, index) => new PlanGap
            {
                PlanId = planId,
                SkillId = gap.SkillId,
                SkillName = gap.Name,
                DeckId = gap.DeckId,
                Shortfall = gap.Shortfall,
                SuggestedJson = RecommendationsJson.Write(gap.SuggestedMemberIds),
                Ordinal = index
            }).ToList()
        };

        db.Plans.Add(plan);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/plans/{plan.Id}", ToDto(plan));
    }

    private static async Task<(RequirementSetDto? Set, ProblemHttpResult? Problem)> LoadSetAsync(
        string requirementId,
        RequirementsClient requirements,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        try
        {
            return (await requirements.GetAsync(requirementId, ct), null);
        }
        catch (Exception exception)
        {
            return (null, Unavailable("Requirements", exception, loggerFactory));
        }
    }

    private static async Task<(TeamDto? Team, ProblemHttpResult? Problem)> LoadTeamAsync(
        RosterClient roster,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        try
        {
            return (await roster.GetTeamAsync(Role, ct), null);
        }
        catch (Exception exception)
        {
            return (null, Unavailable("Roster", exception, loggerFactory));
        }
    }

    private static async Task<(List<MemberProgressDto>? Progress, ProblemHttpResult? Problem)> LoadProgressAsync(
        IEnumerable<MemberDto> members,
        ProgressClient progressClient,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var progress = new List<MemberProgressDto>();

        foreach (var member in members)
        {
            try
            {
                progress.Add(await progressClient.GetProgressAsync(member.Id, ct));
            }
            catch (Exception exception)
            {
                return (null, Unavailable("Progress", exception, loggerFactory));
            }
        }

        return (progress, null);
    }

    private static RecommendationContext BuildContext(
        RequirementSetDto set,
        IReadOnlyList<MemberDto> members,
        IReadOnlyList<MemberProgressDto> progress,
        IReadOnlyList<DeckSummaryDto> decks,
        DateTimeOffset now)
    {
        var requirements = set.Skills
            .Select(skill => new RecommendationRequirement(skill.SkillId, skill.Name, skill.MinLevel, skill.Headcount))
            .ToArray();

        var progressByMember = progress.ToDictionary(entry => entry.MemberId, StringComparer.Ordinal);

        var recommendationMembers = members.Select(member =>
        {
            var levels = new Dictionary<string, double>(StringComparer.Ordinal);

            if (progressByMember.TryGetValue(member.Id, out var memberProgress))
            {
                foreach (var skill in memberProgress.Skills)
                {
                    levels[skill.SkillId] = skill.Percent / 100.0;
                }
            }

            return new RecommendationMember(
                member.Id,
                member.Name,
                member.Colour,
                member.Style,
                member.Worn,
                member.LastActiveAt,
                levels);
        }).ToArray();

        var deckIdBySkill = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var requirement in requirements)
        {
            var deck = decks.FirstOrDefault(candidate => candidate.SkillId == requirement.SkillId);
            if (deck is not null)
            {
                deckIdBySkill[requirement.SkillId] = deck.Id;
            }
        }

        return new RecommendationContext(requirements, recommendationMembers, deckIdBySkill, now);
    }

    private static RecommendedMemberDto ToDto(MemberAssessment assessment) => new(
        assessment.Member.MemberId,
        assessment.Member.Name,
        assessment.Member.Colour,
        assessment.Member.Style,
        assessment.Member.Worn,
        assessment.Readiness,
        assessment.Reasons,
        assessment.Warnings);

    private static PlanDto ToDto(Plan plan) => new(
        plan.Id,
        plan.RequirementId,
        plan.Strategy,
        plan.CreatedAt,
        plan.Members
            .OrderBy(member => member.Ordinal)
            .Select(member => new PlanMemberDto(
                member.MemberId,
                member.Name,
                member.AtLevel,
                RecommendationsJson.Read<IReadOnlyList<string>>(member.WarningsJson, [])))
            .ToArray(),
        plan.Gaps
            .OrderBy(gap => gap.Ordinal)
            .Select(gap => new RecommendationGapDto(
                gap.SkillId,
                gap.SkillName,
                gap.DeckId,
                gap.Shortfall,
                RecommendationsJson.Read<IReadOnlyList<string>>(gap.SuggestedJson, [])))
            .ToArray());

    private static BadRequest<string> UnknownStrategy() =>
        TypedResults.BadRequest("Strategy must be critical, balanced or develop.");

    private static ProblemHttpResult NotConfirmed() => TypedResults.Problem(
        title: "This requirement set is not confirmed",
        detail: "Confirm the requirements before ranking the crew.",
        statusCode: StatusCodes.Status409Conflict);

    private static ProblemHttpResult Unavailable(string service, Exception exception, ILoggerFactory loggerFactory)
    {
        loggerFactory.CreateLogger("Recommendations.Dependencies")
            .LogWarning(exception, "{Service} is unavailable while building a plan", service);

        return TypedResults.Problem(
            title: $"{service} is unavailable",
            detail: $"The plan could not be built because the {service} service did not respond.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?> { ["service"] = service.ToLowerInvariant() });
    }
}
