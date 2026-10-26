using HoneKit.Recommendations.Domain;

namespace HoneKit.Recommendations.Tests;

// The ranking rules on the seeded crew: deterministic ordering, the cost of each strategy, and the
// sentences a leader reads. No EF, no HTTP, no clock.
public sealed class RecommendationRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private static RecommendationRequirement[] FixtureRequirements() =>
    [
        new("framing", "Framing", 0.5, 2),
        new("setup", "Setup", 0.5, 1),
        new("tracking", "Tracking", 0.5, 2)
    ];

    private static RecommendationMember Member(
        string id,
        string name,
        int? daysAgo,
        params (string SkillId, double Level)[] levels) =>
        new(
            id,
            name,
            "teal",
            "backpack",
            false,
            daysAgo is null ? null : Now.AddDays(-daysAgo.Value),
            levels.ToDictionary(level => level.SkillId, level => level.Level, StringComparer.Ordinal));

    private static RecommendationMember[] Crew() =>
    [
        Member("elena", "Elena", 40, ("framing", 1.0), ("setup", 1.0), ("tracking", 1.0), ("presets-traces", 0.5)),
        Member("marcus", "Marcus", 4, ("framing", 0.5), ("setup", 1.0), ("tracking", 0.5), ("presets-traces", 0.5)),
        Member("priya", "Priya", 2, ("framing", 0.5), ("setup", 0.5), ("tracking", 1.0), ("presets-traces", 0.0)),
        Member("noor", "Noor", 9, ("framing", 1.0), ("setup", 0.0), ("tracking", 0.0), ("presets-traces", 0.0)),
        Member("sam", "Sam", 1, ("framing", 0.0), ("setup", 0.0), ("tracking", 0.0), ("presets-traces", 0.5)),
        Member("danny", "Danny", null, ("framing", 0.0), ("setup", 0.0), ("tracking", 0.0), ("presets-traces", 0.0))
    ];

    private static RecommendationContext Context(
        IReadOnlyList<RecommendationRequirement>? requirements = null)
    {
        var decks = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["framing"] = "deck-camera-foundations",
            ["presets-traces"] = "deck-presets-traces"
        };

        return new RecommendationContext(requirements ?? FixtureRequirements(), Crew(), decks, Now);
    }

    private static string[] Names(IEnumerable<MemberAssessment> assessments) =>
        assessments.Select(assessment => assessment.Member.Name).ToArray();

    [Fact]
    public void Critical_prefers_the_crew_that_meets_the_most_requirements()
    {
        var context = Context();

        var suggestions = RecommendationRules.Suggest(context, RecommendationRules.Critical);

        // All of Priya, Marcus and Elena meet everything; recency breaks the tie.
        Assert.Equal(["Priya", "Marcus"], Names(suggestions));
        Assert.All(suggestions, suggestion => Assert.Equal(100, suggestion.Readiness));
    }

    [Fact]
    public void Develop_prefers_members_below_level()
    {
        var context = Context();

        var suggestions = RecommendationRules.Suggest(context, RecommendationRules.Develop);

        Assert.Equal(["Sam", "Danny"], Names(suggestions));
        Assert.Equal(3, suggestions[0].BelowLevelCount);
    }

    [Fact]
    public void Balanced_pairs_the_strongest_with_a_learner()
    {
        var context = Context();

        var suggestions = RecommendationRules.Suggest(context, RecommendationRules.Balanced);

        Assert.Equal(["Priya", "Danny"], Names(suggestions));
    }

    [Fact]
    public void Coverage_reports_what_each_strategy_costs()
    {
        var context = Context();

        var critical = RecommendationRules.Suggest(context, RecommendationRules.Critical);
        var balanced = RecommendationRules.Suggest(context, RecommendationRules.Balanced);
        var develop = RecommendationRules.Suggest(context, RecommendationRules.Develop);

        Assert.Equal(100, RecommendationRules.Coverage(context, critical));
        Assert.Equal(33, RecommendationRules.Coverage(context, balanced));
        Assert.Equal(0, RecommendationRules.Coverage(context, develop));
    }

    [Fact]
    public void A_presets_gap_names_the_new_deck_and_the_below_level_crew()
    {
        var requirements = FixtureRequirements()
            .Append(new RecommendationRequirement("presets-traces", "Presets & Traces", 1.0, 1))
            .ToArray();

        var context = Context(requirements);
        var assessments = RecommendationRules.Assess(context);
        var suggestions = RecommendationRules.Suggest(context, RecommendationRules.Critical);

        var gap = Assert.Single(RecommendationRules.Gaps(context, suggestions, assessments));
        Assert.Equal("presets-traces", gap.SkillId);
        Assert.Equal("Presets & Traces", gap.Name);
        Assert.Equal("deck-presets-traces", gap.DeckId);
        Assert.Equal(1, gap.Shortfall);
        Assert.Equal(
            ["elena", "marcus", "sam", "danny", "noor", "priya"],
            gap.SuggestedMemberIds);
    }

    [Fact]
    public void Sustain_surfaces_a_quiet_member_who_is_at_level()
    {
        var context = Context();

        var sustain = Assert.Single(RecommendationRules.Sustain(context, RecommendationRules.Assess(context)));

        Assert.Equal("elena", sustain.MemberId);
        Assert.Contains("At level on Framing, Setup and Tracking", sustain.Reason, StringComparison.Ordinal);
        Assert.Contains("40 days", sustain.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Warnings_name_every_skill_a_member_is_below()
    {
        var context = Context();
        var assessments = RecommendationRules.Assess(context)
            .ToDictionary(assessment => assessment.Member.MemberId, StringComparer.Ordinal);

        var sam = assessments["sam"];
        Assert.Equal(3, sam.Warnings.Count);
        Assert.Contains(sam.Warnings, warning => warning.Contains("Framing", StringComparison.Ordinal));
        Assert.Contains(sam.Warnings, warning => warning.Contains("Setup", StringComparison.Ordinal));
        Assert.Contains(sam.Warnings, warning => warning.Contains("Tracking", StringComparison.Ordinal));

        Assert.Empty(assessments["elena"].Warnings);
        Assert.True(sam.Reasons.Count is >= 1 and <= 2);
    }

    [Fact]
    public void A_zero_level_requirement_is_met_by_everyone()
    {
        RecommendationRequirement[] requirements = [new("presets-traces", "Presets & Traces", 0, 1)];
        var context = Context(requirements);

        var assessments = RecommendationRules.Assess(context);

        Assert.All(assessments, assessment => Assert.Empty(assessment.Warnings));
        Assert.All(assessments, assessment => Assert.Equal(100, assessment.Readiness));

        var suggestions = RecommendationRules.Suggest(context, RecommendationRules.Critical);
        Assert.Equal(100, RecommendationRules.Coverage(context, suggestions));
        Assert.Empty(RecommendationRules.Gaps(context, suggestions, assessments));
    }
}
