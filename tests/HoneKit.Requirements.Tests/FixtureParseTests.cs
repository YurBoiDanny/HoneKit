using HoneKit.Contracts;
using HoneKit.Requirements.Data;
using HoneKit.Requirements.Domain;
using HoneKit.Requirements.Parsing;

namespace HoneKit.Requirements.Tests;

// The committed replay is the demo's model: if it drifts from the spec's table, these fail.
public sealed class FixtureParseTests
{
    private static readonly SkillDto[] Catalogue =
    [
        new("framing", "camera", "Framing", 2),
        new("setup", "camera", "Setup", 2),
        new("tracking", "camera", "Tracking", 2),
        new("presets-traces", "camera", "Presets & Traces", 2)
    ];

    [Fact]
    public void The_fixture_replays_the_reviewed_parse()
    {
        var draft = RequirementDraft.Normalize(FixtureParse.Load());

        Assert.Empty(RequirementValidation.Validate(draft, Catalogue));

        Assert.Equal("Camera coverage of a group-prayer meeting; podium moved to floor level", draft.Purpose);
        Assert.Equal("high", draft.Criticality);

        Assert.Equal(new[] { "framing", "setup", "tracking" }, draft.Skills.Select(skill => skill.SkillId));
        Assert.Equal((0.5, 2, "crowd camera and floor camera"), (draft.Skills[0].MinLevel, draft.Skills[0].Headcount, draft.Skills[0].Rationale));
        Assert.Equal((0.5, 1, "floor cam set-up"), (draft.Skills[1].MinLevel, draft.Skills[1].Headcount, draft.Skills[1].Rationale));
        Assert.Equal((0.5, 2, "two camera positions"), (draft.Skills[2].MinLevel, draft.Skills[2].Headcount, draft.Skills[2].Rationale));

        Assert.Equal(
            ["PA", "Streaming", "Multimedia", "Audio Recording", "Video Recording", "Review Video Production"],
            draft.OutsideRole);
    }

    [Fact]
    public void The_fixture_is_loaded_from_the_committed_file()
    {
        Assert.False(string.IsNullOrWhiteSpace(FixtureParse.Json));
        Assert.Contains("outsideRole", FixtureParse.Json, StringComparison.Ordinal);
        Assert.Contains("Review Video Production", FixtureParse.Json, StringComparison.Ordinal);
    }

    [Fact]
    public void A_parse_failure_carries_the_source_and_errors()
    {
        var result = ParseResult.Failed(
            statusCode: 503,
            detail: "Reading is unavailable right now.",
            sourceName: "cx-rollout.pdf",
            sourceText: "the extracted brief",
            errors: ["No model key is configured."]);

        Assert.False(result.Succeeded);
        Assert.Null(result.Draft);

        var failure = result.Failure!;
        Assert.Equal(503, failure.StatusCode);
        Assert.Equal("Reading is unavailable right now.", failure.Body.Detail);
        Assert.Equal("cx-rollout.pdf", failure.Body.SourceName);
        Assert.Equal("the extracted brief", failure.Body.SourceText);
        Assert.Equal(["No model key is configured."], failure.Body.Errors);
    }
}
