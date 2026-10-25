using HoneKit.Contracts;

namespace HoneKit.Roster.Domain;

// Pure rules, no HTTP and no EF: slice 3 builds on this and it stays unit-testable.
public static class BagRules
{
    /// <summary>
    /// A skill is earned when every one of its lessons appears in the member's completions.
    /// Unknown lesson ids are ignored; a skill with no lessons is never earned.
    /// </summary>
    public static IReadOnlyList<string> EarnedSkillIds(
        IEnumerable<string> completedLessonIds,
        IEnumerable<LessonDto> lessons,
        IEnumerable<SkillDto> skills)
    {
        var completed = completedLessonIds.ToHashSet(StringComparer.Ordinal);
        var lessonsBySkill = lessons
            .GroupBy(lesson => lesson.SkillId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(lesson => lesson.Id).ToArray(), StringComparer.Ordinal);

        return skills
            .Where(skill =>
                lessonsBySkill.TryGetValue(skill.Id, out var skillLessons)
                && skillLessons.Length > 0
                && skillLessons.All(completed.Contains))
            .Select(skill => skill.Id)
            .ToArray();
    }
}
