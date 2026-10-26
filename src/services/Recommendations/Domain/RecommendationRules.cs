using HoneKit.Contracts;

namespace HoneKit.Recommendations.Domain;

public sealed record RecommendationRequirement(string SkillId, string Name, double MinLevel, int Headcount);

public sealed record RecommendationMember(
    string MemberId,
    string Name,
    string Colour,
    string Style,
    bool Worn,
    DateTimeOffset? LastActiveAt,
    IReadOnlyDictionary<string, double> LevelBySkill);

public sealed record RecommendationContext(
    IReadOnlyList<RecommendationRequirement> Requirements,
    IReadOnlyList<RecommendationMember> Members,
    IReadOnlyDictionary<string, string> DeckIdBySkill,
    DateTimeOffset Now);

/// <summary>A member judged against this meeting's requirements, with the sentences shown beside them.</summary>
public sealed record MemberAssessment(
    RecommendationMember Member,
    double Readiness,
    int SkillsMet,
    int BelowLevelCount,
    IReadOnlyList<string> AtLevelSkillIds,
    IReadOnlyList<string> BelowLevelSkillIds,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Warnings);

/// <summary>
/// The ranking rules, over plain data: no EF, no HTTP, no clock. The endpoint maps API payloads to
/// this shape; the tests drive it directly. Everything here is deterministic.
/// </summary>
public static class RecommendationRules
{
    public const string Critical = "critical";

    public const string Balanced = "balanced";

    public const string Develop = "develop";

    public const int QuietDays = 30;

    public static IReadOnlyList<string> Strategies { get; } = [Critical, Balanced, Develop];

    public static bool IsStrategy(string? strategy) =>
        strategy is not null && Strategies.Contains(strategy, StringComparer.Ordinal);

    /// <summary>level / requirement, capped at 1 for readiness; a zero requirement is always met.</summary>
    public static IReadOnlyList<MemberAssessment> Assess(RecommendationContext context) =>
        context.Members.Select(member => AssessMember(context, member)).ToArray();

    /// <summary>The meeting's places: the largest headcount asked for, at least one.</summary>
    public static int SuggestionCount(RecommendationContext context) =>
        context.Requirements.Count == 0 ? 0 : Math.Max(1, context.Requirements.Max(requirement => requirement.Headcount));

    public static IReadOnlyList<MemberAssessment> Suggest(RecommendationContext context, string strategy)
    {
        var count = SuggestionCount(context);
        if (count == 0)
        {
            return [];
        }

        var assessments = Assess(context);
        var criticalOrder = CriticalOrder(assessments).ToArray();

        var ordered = strategy switch
        {
            Critical => criticalOrder,
            Develop => DevelopOrder(assessments).ToArray(),
            Balanced => BalancedOrder(criticalOrder, count),
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown strategy.")
        };

        return ordered.Take(count).ToArray();
    }

    /// <summary>Share of required skills with at least the asked-for headcount at level among the chosen.</summary>
    public static double Coverage(RecommendationContext context, IReadOnlyList<MemberAssessment> chosen)
    {
        if (context.Requirements.Count == 0)
        {
            return 0;
        }

        var covered = context.Requirements.Count(requirement =>
            chosen.Count(assessment => assessment.AtLevelSkillIds.Contains(requirement.SkillId)) >= requirement.Headcount);

        return Math.Round(covered * 100.0 / context.Requirements.Count);
    }

    /// <summary>
    /// Skills the chosen crew cannot cover. A skill that is covered is not a gap; a shortfall names
    /// the members below level, best first, so the deck can be sent somewhere useful.
    /// </summary>
    public static IReadOnlyList<RecommendationGapDto> Gaps(
        RecommendationContext context,
        IReadOnlyList<MemberAssessment> chosen,
        IReadOnlyList<MemberAssessment> assessments)
    {
        var gaps = new List<RecommendationGapDto>();

        foreach (var requirement in context.Requirements)
        {
            var atLevel = chosen.Count(assessment => assessment.AtLevelSkillIds.Contains(requirement.SkillId));
            var shortfall = Math.Max(0, requirement.Headcount - atLevel);

            if (shortfall == 0)
            {
                continue;
            }

            context.DeckIdBySkill.TryGetValue(requirement.SkillId, out var deckId);

            var suggested = assessments
                .Where(assessment => assessment.BelowLevelSkillIds.Contains(requirement.SkillId))
                .OrderByDescending(assessment => Level(assessment.Member, requirement.SkillId))
                .ThenBy(assessment => assessment.Member.Name, StringComparer.Ordinal)
                .Select(assessment => assessment.Member.MemberId)
                .ToArray();

            gaps.Add(new RecommendationGapDto(requirement.SkillId, requirement.Name, deckId, shortfall, suggested));
        }

        return gaps;
    }

    /// <summary>At level on something required but quiet for over a month: a refresher, not a gap.</summary>
    public static IReadOnlyList<SustainDto> Sustain(
        RecommendationContext context,
        IReadOnlyList<MemberAssessment> assessments)
    {
        var sustain = new List<SustainDto>();

        foreach (var assessment in assessments)
        {
            if (assessment.AtLevelSkillIds.Count == 0 || assessment.Member.LastActiveAt is null)
            {
                continue;
            }

            var days = (int)Math.Floor((context.Now - assessment.Member.LastActiveAt.Value).TotalDays);
            if (days <= QuietDays)
            {
                continue;
            }

            var skills = context.Requirements
                .Where(requirement => assessment.AtLevelSkillIds.Contains(requirement.SkillId))
                .Select(requirement => requirement.Name)
                .ToArray();

            sustain.Add(new SustainDto(
                assessment.Member.MemberId,
                assessment.Member.Name,
                $"At level on {Join(skills)}, but hasn't answered in {days} days."));
        }

        return sustain;
    }

    public static double Level(RecommendationMember member, string skillId) =>
        member.LevelBySkill.TryGetValue(skillId, out var level) ? level : 0;

    private static MemberAssessment AssessMember(RecommendationContext context, RecommendationMember member)
    {
        var atLevel = new List<string>();
        var belowLevel = new List<string>();
        var contributions = new List<double>();

        foreach (var requirement in context.Requirements)
        {
            var level = Level(member, requirement.SkillId);
            var met = requirement.MinLevel <= 0 || level >= requirement.MinLevel;

            if (met)
            {
                atLevel.Add(requirement.SkillId);
            }
            else
            {
                belowLevel.Add(requirement.SkillId);
            }

            contributions.Add(requirement.MinLevel <= 0 ? 1 : Math.Min(level / requirement.MinLevel, 1));
        }

        var readiness = contributions.Count == 0 ? 0 : Math.Round(contributions.Average() * 100);

        var warnings = belowLevel
            .Select(skillId => $"Below this meeting's requirement on {NameOf(context, skillId)}.")
            .ToArray();

        var reasons = new List<string> { StandingSentence(context, atLevel, belowLevel) };
        reasons.Add(RecencySentence(member, context.Now));

        return new MemberAssessment(
            member,
            readiness,
            atLevel.Count,
            belowLevel.Count,
            atLevel,
            belowLevel,
            reasons,
            warnings);
    }

    private static string StandingSentence(
        RecommendationContext context,
        IReadOnlyList<string> atLevel,
        IReadOnlyList<string> belowLevel)
    {
        var atLevelNames = atLevel.Select(skillId => NameOf(context, skillId)).ToArray();
        var belowLevelNames = belowLevel.Select(skillId => NameOf(context, skillId)).ToArray();

        return (atLevelNames.Length, belowLevelNames.Length) switch
        {
            (0, 0) => "No required skills on this plan.",
            (0, _) => $"Below level on {Join(belowLevelNames)}.",
            (_, 0) => $"At level on {Join(atLevelNames)}.",
            _ => $"At level on {Join(atLevelNames)}; below level on {Join(belowLevelNames)}."
        };
    }

    private static string RecencySentence(RecommendationMember member, DateTimeOffset now)
    {
        if (member.LastActiveAt is null)
        {
            return "Hasn't answered a card yet.";
        }

        var days = (int)Math.Floor((now - member.LastActiveAt.Value).TotalDays);

        return days switch
        {
            <= 0 => "Answered today.",
            1 => "Last answered 1 day ago.",
            _ => $"Last answered {days} days ago."
        };
    }

    private static IEnumerable<MemberAssessment> CriticalOrder(IEnumerable<MemberAssessment> assessments) =>
        assessments
            .OrderByDescending(assessment => assessment.SkillsMet)
            .ThenByDescending(assessment => assessment.Readiness)
            .ThenByDescending(assessment => assessment.Member.LastActiveAt ?? DateTimeOffset.MinValue)
            .ThenBy(assessment => assessment.Member.Name, StringComparer.Ordinal);

    private static IEnumerable<MemberAssessment> DevelopOrder(IEnumerable<MemberAssessment> assessments) =>
        assessments
            .OrderByDescending(assessment => assessment.BelowLevelCount)
            .ThenBy(assessment => assessment.Readiness)
            .ThenByDescending(assessment => assessment.Member.LastActiveAt ?? DateTimeOffset.MinValue)
            .ThenBy(assessment => assessment.Member.Name, StringComparer.Ordinal);

    /// <summary>
    /// The critical-best first, then someone below level on a skill that member covers, so a strong
    /// hand carries a learner. Falls back to the critical order when nobody pairs.
    /// </summary>
    private static IReadOnlyList<MemberAssessment> BalancedOrder(IReadOnlyList<MemberAssessment> criticalOrder, int count)
    {
        var chosen = new List<MemberAssessment>();
        if (criticalOrder.Count == 0)
        {
            return chosen;
        }

        chosen.Add(criticalOrder[0]);

        while (chosen.Count < count && chosen.Count < criticalOrder.Count)
        {
            var first = chosen[0];

            var remaining = criticalOrder
                .Where(assessment => chosen.All(picked => picked.Member.MemberId != assessment.Member.MemberId))
                .ToArray();

            var pairing = remaining
                .Where(assessment => first.AtLevelSkillIds.Any(skillId =>
                    assessment.BelowLevelSkillIds.Contains(skillId)))
                .OrderByDescending(assessment => assessment.BelowLevelCount)
                .ThenByDescending(assessment => assessment.Readiness)
                .ThenBy(assessment => assessment.Member.Name, StringComparer.Ordinal)
                .ToArray();

            chosen.Add(pairing.Length > 0 ? pairing[0] : remaining[0]);
        }

        return chosen;
    }

    private static string NameOf(RecommendationContext context, string skillId) =>
        context.Requirements.FirstOrDefault(requirement => requirement.SkillId == skillId)?.Name ?? skillId;

    private static string Join(IReadOnlyList<string> names) => names.Count switch
    {
        0 => "",
        1 => names[0],
        2 => $"{names[0]} and {names[1]}",
        _ => $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}"
    };
}
