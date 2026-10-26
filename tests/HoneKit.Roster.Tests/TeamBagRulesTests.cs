using HoneKit.Contracts;
using HoneKit.Roster.Domain;

namespace HoneKit.Roster.Tests;

// The aggregate read, checked against the rule the PRD states and the numbers the seed promises.
public sealed class TeamBagRulesTests
{
    private static readonly LessonDto[] FramingLessons =
    [
        new("framing-1", "framing", 1, "Reading the frame"),
        new("framing-2", "framing", 2, "Light and colour")
    ];

    private static readonly SkillDto FramingSkill = new("framing", "camera", "Framing", 2);

    [Fact]
    public void Uneven_completions_fill_the_tool_proportionally()
    {
        // Two members, one skill, two lessons each: 2 + 1 completed of 4 slots.
        var tools = TeamBagRules.Tools(
            completionsPerMember: [["framing-1", "framing-2"], ["framing-1"]],
            lessons: FramingLessons,
            skills: [FramingSkill],
            memberCount: 2);

        var tool = Assert.Single(tools);
        Assert.Equal(3, tool.FilledLessons);
        Assert.Equal(4, tool.TotalLessons);
        Assert.Equal(75, tool.Percent);
    }

    [Fact]
    public void A_skill_with_no_completions_reads_zero()
    {
        var tools = TeamBagRules.Tools(
            completionsPerMember: [["framing-1", "framing-2"], ["framing-2"]],
            lessons: FramingLessons.Append(new LessonDto("setup-1", "setup", 1, "Build the camera")),
            skills: [FramingSkill, new SkillDto("setup", "camera", "Setup", 2)],
            memberCount: 2);

        var setup = tools.Single(tool => tool.SkillId == "setup");
        Assert.Equal(0, setup.FilledLessons);
        Assert.Equal(4, setup.TotalLessons);
        Assert.Equal(0, setup.Percent);
    }

    [Fact]
    public void A_member_with_no_completions_still_counts_towards_the_total()
    {
        var tools = TeamBagRules.Tools(
            completionsPerMember: [["framing-1", "framing-2"], [], ["framing-1"]],
            lessons: FramingLessons,
            skills: [FramingSkill],
            memberCount: 3);

        var tool = Assert.Single(tools);
        Assert.Equal(3, tool.FilledLessons);
        Assert.Equal(6, tool.TotalLessons);
        Assert.Equal(50, tool.Percent);
    }

    [Fact]
    public void No_members_means_no_division_by_zero()
    {
        var tools = TeamBagRules.Tools(
            completionsPerMember: [],
            lessons: FramingLessons,
            skills: [FramingSkill],
            memberCount: 0);

        var tool = Assert.Single(tools);
        Assert.Equal(0, tool.FilledLessons);
        Assert.Equal(0, tool.TotalLessons);
        Assert.Equal(0, tool.Percent);
    }

    [Fact]
    public void The_seeded_crew_reads_nineteen_of_forty_eight()
    {
        // The Progress seed, member by member.
        List<IReadOnlyCollection<string>> completions =
        [
            ["framing-1", "framing-2", "setup-1", "setup-2", "tracking-1", "tracking-2", "presets-traces-1"],
            ["framing-1", "setup-1", "setup-2", "tracking-1", "presets-traces-1"],
            ["framing-1", "setup-1", "tracking-1", "tracking-2"],
            ["framing-1", "framing-2"],
            ["presets-traces-1"],
            []
        ];

        LessonDto[] lessons =
        [
            .. FramingLessons,
            new("setup-1", "setup", 1, "Build the camera"),
            new("setup-2", "setup", 2, "The floor camera"),
            new("tracking-1", "tracking", 1, "Hold the subject"),
            new("tracking-2", "tracking", 2, "Follow with intent"),
            new("presets-traces-1", "presets-traces", 1, "Presets on the Bolin"),
            new("presets-traces-2", "presets-traces", 2, "Traces and tracking")
        ];

        SkillDto[] skills =
        [
            new("framing", "camera", "Framing", 2),
            new("setup", "camera", "Setup", 2),
            new("tracking", "camera", "Tracking", 2),
            new("presets-traces", "camera", "Presets & Traces", 2)
        ];

        var tools = TeamBagRules.Tools(completions, lessons, skills, memberCount: 6);

        // Sorted by name, which is how the bag and the team bag show them.
        Assert.Equal(["Framing", "Presets & Traces", "Setup", "Tracking"], tools.Select(tool => tool.Name));
        Assert.Equal((6, 12, 50.0), (tools[0].FilledLessons, tools[0].TotalLessons, tools[0].Percent));
        Assert.Equal((3, 12, 25.0), (tools[1].FilledLessons, tools[1].TotalLessons, tools[1].Percent));
        Assert.Equal((5, 12, 42.0), (tools[2].FilledLessons, tools[2].TotalLessons, tools[2].Percent));
        Assert.Equal((5, 12, 42.0), (tools[3].FilledLessons, tools[3].TotalLessons, tools[3].Percent));

        var filled = tools.Sum(tool => tool.FilledLessons);
        var total = tools.Sum(tool => tool.TotalLessons);
        Assert.Equal(19, filled);
        Assert.Equal(48, total);
        Assert.Equal(40, Math.Round(filled * 100.0 / total));
    }

    [Fact]
    public void One_worn_member_makes_the_whole_bag_worn()
    {
        Assert.Equal("worn", TeamBagRules.Wear([false, false, true, false]));
        Assert.Equal("kept", TeamBagRules.Wear([false, false, false]));
        Assert.Equal("kept", TeamBagRules.Wear([]));
    }
}
