using HoneKit.Contracts;

namespace HoneKit.Roster.Domain;

/// <summary>
/// The leader's read: one tool per skill, filled by the share of that skill's lessons the role has
/// completed. Pure arithmetic, so the seed maths and the boundaries are testable without EF or HTTP.
/// </summary>
public static class TeamBagRules
{
    public const string Worn = "worn";

    public const string Kept = "kept";

    /// <summary>
    /// Fill for a skill is every completed lesson of that skill across the role's members, over
    /// (member count × the skill's lesson count) — the PRD's "total completions ÷ (members × lessons)".
    /// A member's duplicate ids are counted once, and unknown lesson ids belong to no skill.
    /// </summary>
    public static IReadOnlyList<TeamToolDto> Tools(
        IEnumerable<IReadOnlyCollection<string>> completionsPerMember,
        IEnumerable<LessonDto> lessons,
        IEnumerable<SkillDto> skills,
        int memberCount)
    {
        var memberCompletions = completionsPerMember as IReadOnlyList<IReadOnlyCollection<string>>
            ?? completionsPerMember.ToArray();

        var lessonIdsBySkill = lessons
            .DistinctBy(lesson => lesson.Id)
            .GroupBy(lesson => lesson.SkillId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(lesson => lesson.Id).ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);

        return skills
            .DistinctBy(skill => skill.Id)
            .OrderBy(skill => skill.Name, StringComparer.Ordinal)
            .ThenBy(skill => skill.Id, StringComparer.Ordinal)
            .Select(skill =>
            {
                var lessonIds = lessonIdsBySkill.TryGetValue(skill.Id, out var ids)
                    ? ids
                    : new HashSet<string>(StringComparer.Ordinal);

                var filledLessons = memberCompletions.Sum(completed =>
                    completed.Distinct(StringComparer.Ordinal).Count(lessonIds.Contains));

                var totalLessons = memberCount * skill.LessonCount;
                var percent = totalLessons <= 0 ? 0 : Math.Round(filledLessons * 100.0 / totalLessons);

                return new TeamToolDto(skill.Id, skill.Name, filledLessons, totalLessons, percent);
            })
            .ToArray();
    }

    /// <summary>Wear is a property of the crew, not a score: one quiet member makes the bag worn.</summary>
    public static string Wear(IEnumerable<bool> wornFlags) =>
        wornFlags.Any(worn => worn) ? Worn : Kept;
}
