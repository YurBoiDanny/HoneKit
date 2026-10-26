using HoneKit.Contracts;

namespace HoneKit.Progress.Domain;

/// <summary>One graded answer, in the order the member gave it.</summary>
public readonly record struct GradedAnswer(string CardId, bool Correct);

/// <summary>
/// Everything the finish screen needs, computed from the attempts and what is left in the queue.
/// </summary>
public sealed record SessionOutcome(
    int FirstTryCorrect,
    int TotalCards,
    int Retries,
    double ScorePercent,
    bool Passed,
    bool Honed,
    bool Completed,
    bool ToolGranted);

/// <summary>
/// The session rules, with no HTTP and no EF: queue order, first-try counting, pass/honed, completion.
/// The endpoints translate to and from this; the tests target it directly.
/// </summary>
public static class ProgressDomain
{
    /// <summary>A wrong card comes back after two other cards, or at the end when fewer remain.</summary>
    public const int RequeueOffset = 2;

    public const double PassThreshold = 80;

    public const double HonedThreshold = 90;

    public const int HonedRetryLimit = 2;

    public static readonly TimeSpan CorrectInterval = TimeSpan.FromDays(3);

    public static readonly TimeSpan WrongInterval = TimeSpan.FromMinutes(10);

    /// <summary>The card at the front of the queue, or null when the deck is done.</summary>
    public static string? Next(IReadOnlyList<string> queue) => queue.Count == 0 ? null : queue[0];

    public static bool IsComplete(IReadOnlyList<string> queue) => queue.Count == 0;

    public static IReadOnlyList<string> Remove(IReadOnlyList<string> queue, string cardId)
    {
        var remaining = queue.ToList();
        remaining.Remove(cardId);
        return remaining;
    }

    /// <summary>Where a wrong card lands in a queue that no longer contains it.</summary>
    public static int RequeueIndex(int remainingCount) => Math.Min(RequeueOffset, remainingCount);

    /// <summary>
    /// Puts a wrong card back after two other cards. <paramref name="queue"/> must already have the
    /// card removed, otherwise it would be counted as one of the two cards it is hidden behind.
    /// </summary>
    public static IReadOnlyList<string> RequeueAfterTwo(IReadOnlyList<string> queue, string cardId)
    {
        var list = queue.ToList();
        list.Insert(RequeueIndex(list.Count), cardId);
        return list;
    }

    /// <summary>
    /// A card counts on its first answer only. Wrong-then-correct is still not a first-try correct.
    /// </summary>
    public static int FirstTryCorrectCount(IEnumerable<GradedAnswer> answers)
    {
        var firstAnswers = new Dictionary<string, bool>(StringComparer.Ordinal);

        foreach (var answer in answers)
        {
            firstAnswers.TryAdd(answer.CardId, answer.Correct);
        }

        return firstAnswers.Values.Count(correct => correct);
    }

    public static bool IsPassed(double scorePercent) => scorePercent >= PassThreshold;

    /// <summary>Honed is the quality label: near-perfect firsts and almost no retries.</summary>
    public static bool IsHoned(double scorePercent, int retries) =>
        scorePercent >= HonedThreshold && retries <= HonedRetryLimit;

    /// <summary>
    /// The finish rules in one place. Retries are every attempt beyond the first for its card.
    /// The tool is granted by completing the deck, not by passing it: pass is the quality label.
    /// </summary>
    public static SessionOutcome Evaluate(IEnumerable<GradedAnswer> answers, IReadOnlyList<string> queue)
    {
        var all = answers as IReadOnlyList<GradedAnswer> ?? answers.ToArray();
        var totalCards = all.Select(answer => answer.CardId).Distinct(StringComparer.Ordinal).Count();
        var firstTryCorrect = FirstTryCorrectCount(all);
        var retries = all.Count - totalCards;

        var scorePercent = totalCards == 0
            ? 0
            : Math.Round(firstTryCorrect * 100.0 / totalCards);

        var completed = IsComplete(queue);

        return new SessionOutcome(
            firstTryCorrect,
            totalCards,
            retries,
            scorePercent,
            IsPassed(scorePercent),
            IsHoned(scorePercent, retries),
            completed,
            ToolGranted: completed);
    }

    /// <summary>
    /// The skills a member has honed at least once. A later plain run never takes an earlier honed
    /// result away, and decks Content no longer knows are ignored.
    /// </summary>
    public static IReadOnlyList<string> HonedSkillIds(
        IEnumerable<(string DeckId, bool Honed)> sessions,
        IReadOnlyDictionary<string, string> skillIdByDeckId) =>
        sessions
            .Where(session => session.Honed)
            .Select(session => skillIdByDeckId.TryGetValue(session.DeckId, out var skillId) ? skillId : null)
            .Where(skillId => !string.IsNullOrEmpty(skillId))
            .Select(skillId => skillId!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(skillId => skillId, StringComparer.Ordinal)
            .ToArray();

    public static DateTimeOffset DueAt(bool correct, DateTimeOffset now) =>
        now.Add(correct ? CorrectInterval : WrongInterval);

    /// <summary>
    /// Per-skill progress over the role's catalogue: completed lessons over the skill's lesson
    /// count, with the most recent completion as recency. Unknown members simply have no completions.
    /// </summary>
    public static IReadOnlyList<SkillProgressDto> SkillProgress(
        IEnumerable<(string LessonId, DateTimeOffset CompletedAt)> completions,
        IEnumerable<SkillDto> skills,
        IEnumerable<LessonDto> lessons)
    {
        var completedAtByLesson = completions
            .GroupBy(completion => completion.LessonId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Max(completion => completion.CompletedAt),
                StringComparer.Ordinal);

        var lessonIdsBySkill = lessons
            .DistinctBy(lesson => lesson.Id)
            .GroupBy(lesson => lesson.SkillId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(lesson => lesson.Id).ToArray(),
                StringComparer.Ordinal);

        return skills
            .DistinctBy(skill => skill.Id)
            .OrderBy(skill => skill.Name, StringComparer.Ordinal)
            .ThenBy(skill => skill.Id, StringComparer.Ordinal)
            .Select(skill =>
            {
                var lessonIds = lessonIdsBySkill.TryGetValue(skill.Id, out var known) ? known : [];
                var completed = lessonIds.Where(completedAtByLesson.ContainsKey).ToArray();

                var percent = skill.LessonCount <= 0
                    ? 0
                    : Math.Round(completed.Length * 100.0 / skill.LessonCount);

                DateTimeOffset? lastAnsweredAt = completed.Length == 0
                    ? null
                    : completed.Max(lessonId => completedAtByLesson[lessonId]);

                return new SkillProgressDto(
                    skill.Id,
                    skill.Name,
                    completed.Length,
                    skill.LessonCount,
                    percent,
                    lastAnsweredAt);
            })
            .ToArray();
    }
}
