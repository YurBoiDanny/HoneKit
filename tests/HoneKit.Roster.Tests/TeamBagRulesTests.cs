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
            lessons: FramingLessons.Append(new LessonDto("gear-1", "gear", 1, "Build the camera")),
            skills: [FramingSkill, new SkillDto("gear", "camera", "Gear", 2)],
            memberCount: 2);

        var gear = tools.Single(tool => tool.SkillId == "gear");
        Assert.Equal(0, gear.FilledLessons);
        Assert.Equal(4, gear.TotalLessons);
        Assert.Equal(0, gear.Percent);
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
            ["framing-1", "framing-2", "gear-1", "gear-2", "operation-1", "operation-2", "switching-1"],
            ["framing-1", "gear-1", "gear-2", "operation-1", "switching-1"],
            ["framing-1", "gear-1", "operation-1", "operation-2"],
            ["framing-1", "framing-2"],
            ["switching-1"],
            []
        ];

        LessonDto[] lessons =
        [
            .. FramingLessons,
            new("gear-1", "gear", 1, "Build the camera"),
            new("gear-2", "gear", 2, "Know the kit"),
            new("operation-1", "operation", 1, "Run the service"),
            new("operation-2", "operation", 2, "Recover on the fly"),
            new("switching-1", "switching", 1, "Cut with intent"),
            new("switching-2", "switching", 2, "Switch under pressure")
        ];

        SkillDto[] skills =
        [
            new("framing", "camera", "Framing", 2),
            new("gear", "camera", "Gear", 2),
            new("operation", "camera", "Operation", 2),
            new("switching", "camera", "Switching", 2)
        ];

        var tools = TeamBagRules.Tools(completions, lessons, skills, memberCount: 6);

        Assert.Equal(["Framing", "Gear", "Operation", "Switching"], tools.Select(tool => tool.Name));
        Assert.Equal((6, 12, 50.0), (tools[0].FilledLessons, tools[0].TotalLessons, tools[0].Percent));
        Assert.Equal((5, 12, 42.0), (tools[1].FilledLessons, tools[1].TotalLessons, tools[1].Percent));
        Assert.Equal((5, 12, 42.0), (tools[2].FilledLessons, tools[2].TotalLessons, tools[2].Percent));
        Assert.Equal((3, 12, 25.0), (tools[3].FilledLessons, tools[3].TotalLessons, tools[3].Percent));

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
