using HoneKit.Contracts;
using HoneKit.Roster.Domain;

namespace HoneKit.Roster.Tests;

// Derivation and mapping only: no HTTP and no database. A tool exists because the lessons are
// complete, and honed/acknowledged only decorate the tools that derivation already produced.
public sealed class BagRulesTests
{
    private static readonly SkillDto[] Skills =
    [
        new("framing", "camera", "Framing", 2),
        new("setup", "camera", "Setup", 2),
        new("empty", "camera", "Empty skill", 0)
    ];

    private static readonly LessonDto[] Lessons =
    [
        new("framing-1", "framing", 1, "Reading the frame"),
        new("framing-2", "framing", 2, "Light and colour"),
        new("setup-1", "setup", 1, "Build the camera"),
        new("setup-2", "setup", 2, "The floor camera")
    ];

    [Fact]
    public void Every_lesson_complete_earns_the_skill()
    {
        var earned = BagRules.EarnedSkillIds(
            ["framing-1", "framing-2", "setup-1", "setup-2"],
            Lessons,
            Skills);

        Assert.Equal(new[] { "framing", "setup" }, earned);
    }

    [Fact]
    public void One_lesson_short_does_not_earn_the_skill()
    {
        var earned = BagRules.EarnedSkillIds(
            ["framing-1", "framing-2", "setup-1"],
            Lessons,
            Skills);

        Assert.Equal(new[] { "framing" }, earned);
    }

    [Fact]
    public void Unknown_completed_lesson_ids_are_ignored()
    {
        var earned = BagRules.EarnedSkillIds(
            ["framing-1", "framing-2", "unknown-9"],
            Lessons,
            Skills);

        Assert.Equal(new[] { "framing" }, earned);
    }

    [Fact]
    public void A_skill_with_no_lessons_is_never_earned()
    {
        var earned = BagRules.EarnedSkillIds([], Lessons, Skills);

        Assert.Empty(earned);
    }

    [Fact]
    public void Honed_and_acknowledged_decorate_only_earned_tools()
    {
        // setup is honed in Progress but the member has not earned it, so it must not appear.
        var tools = BagRules.Tools(
            earnedSkillIds: ["framing"],
            skills: Skills,
            honedSkillIds: ["framing", "setup"],
            acknowledgedSkillIds: ["framing", "setup"]);

        var tool = Assert.Single(tools);
        Assert.Equal("framing", tool.SkillId);
        Assert.Equal("Framing", tool.Name);
        Assert.True(tool.Honed);
        Assert.True(tool.Acknowledged);
    }

    [Fact]
    public void Unhoned_and_unacknowledged_tools_stay_plain()
    {
        var tools = BagRules.Tools(
            earnedSkillIds: ["setup", "framing"],
            skills: Skills,
            honedSkillIds: ["framing"],
            acknowledgedSkillIds: []);

        Assert.Equal(new[] { "Framing", "Setup" }, tools.Select(tool => tool.Name));
        Assert.True(tools[0].Honed);
        Assert.False(tools[0].Acknowledged);
        Assert.False(tools[1].Honed);
        Assert.False(tools[1].Acknowledged);
    }
}
