using HoneKit.Contracts;
using HoneKit.Requirements.Domain;

namespace HoneKit.Requirements.Tests;

// The rules every requirement set passes through, whether a model drafted it or a leader typed it.
public sealed class RequirementValidationTests
{
    private static readonly SkillDto[] Catalogue =
    [
        new("framing", "camera", "Framing", 2),
        new("gear", "camera", "Gear", 2),
        new("operation", "camera", "Operation", 2),
        new("switching", "camera", "Switching", 2)
    ];

    private static ParsedRequirements ValidDraft() => new()
    {
        Purpose = "Camera coverage of a group-prayer meeting",
        Criticality = "high",
        Skills = [new("framing", 0.5, 2, "crowd camera and floor camera")],
        OutsideRole = ["PA", "Streaming"]
    };

    [Fact]
    public void A_valid_set_passes()
    {
        Assert.Empty(RequirementValidation.Validate(ValidDraft(), Catalogue));
    }

    [Fact]
    public void An_unknown_skill_is_rejected()
    {
        var draft = ValidDraft();
        draft.Skills = [new("pa", 0.5, 1, "the brief asks for it")];

        var errors = RequirementValidation.Validate(draft, Catalogue);

        Assert.Contains(errors, error => error.Contains("'pa'", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0.25)]
    [InlineData(1.5)]
    public void A_level_outside_the_dial_is_rejected(double level)
    {
        var draft = ValidDraft();
        draft.Skills = [new("framing", level, 2, "crowd camera")];

        var errors = RequirementValidation.Validate(draft, Catalogue);

        Assert.Contains(errors, error => error.Contains("minimum level", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void A_headcount_outside_the_range_is_rejected(int headcount)
    {
        var draft = ValidDraft();
        draft.Skills = [new("framing", 0.5, headcount, "crowd camera")];

        var errors = RequirementValidation.Validate(draft, Catalogue);

        Assert.Contains(errors, error => error.Contains("headcount", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void A_duplicate_skill_is_rejected()
    {
        var draft = ValidDraft();
        draft.Skills =
        [
            new("framing", 0.5, 2, "crowd camera"),
            new("framing", 1, 1, "floor camera")
        ];

        var errors = RequirementValidation.Validate(draft, Catalogue);

        Assert.Contains(errors, error => error.Contains("listed twice", StringComparison.Ordinal));
    }

    [Fact]
    public void An_empty_purpose_is_rejected()
    {
        var draft = ValidDraft();
        draft.Purpose = "   ";

        var errors = RequirementValidation.Validate(draft, Catalogue);

        Assert.Contains(errors, error => error.Contains("purpose", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void An_unknown_criticality_is_rejected()
    {
        var draft = ValidDraft();
        draft.Criticality = "urgent";

        var errors = RequirementValidation.Validate(draft, Catalogue);

        Assert.Contains(errors, error => error.Contains("Criticality", StringComparison.Ordinal));
    }
}
