using HoneKit.Progress.Domain;

namespace HoneKit.Progress.Tests;

// The rules only: no HTTP, no EF, no database. If a rule needs a shape from the endpoints, that
// shape is translated into GradedAnswer at the boundary.
public sealed class ProgressDomainTests
{
    [Fact]
    public void Wrong_card_returns_after_two_other_cards()
    {
        var remaining = ProgressDomain.Remove(["a", "b", "c", "d"], "a");

        var queue = ProgressDomain.RequeueAfterTwo(remaining, "a");

        Assert.Equal(new[] { "b", "c", "a", "d" }, queue);
        Assert.Equal(2, queue.ToList().IndexOf("a"));
    }

    [Fact]
    public void Wrong_card_lands_at_the_end_when_fewer_cards_remain()
    {
        Assert.Equal(new[] { "b", "a" }, ProgressDomain.RequeueAfterTwo(["b"], "a"));
        Assert.Equal(new[] { "a" }, ProgressDomain.RequeueAfterTwo([], "a"));
    }

    [Fact]
    public void A_card_counts_on_its_first_answer_only()
    {
        // headroom wrong then right; white balance right first time.
        GradedAnswer[] answers =
        [
            new("headroom", false),
            new("white-balance", true),
            new("headroom", true)
        ];

        Assert.Equal(1, ProgressDomain.FirstTryCorrectCount(answers));

        var outcome = ProgressDomain.Evaluate(answers, []);

        Assert.Equal(1, outcome.FirstTryCorrect);
        Assert.Equal(2, outcome.TotalCards);
        Assert.Equal(1, outcome.Retries);
        Assert.Equal(50, outcome.ScorePercent);
        Assert.False(outcome.Passed);
    }

    [Theory]
    [InlineData(79, false)]
    [InlineData(80, true)]
    public void Pass_boundary_is_eighty_percent(double scorePercent, bool passed)
    {
        Assert.Equal(passed, ProgressDomain.IsPassed(scorePercent));
    }

    [Theory]
    [InlineData(89, 0, false)]
    [InlineData(90, 0, true)]
    [InlineData(90, 2, true)]
    [InlineData(90, 3, false)]
    [InlineData(100, 0, true)]
    public void Honed_needs_ninety_percent_and_at_most_two_retries(double scorePercent, int retries, bool honed)
    {
        Assert.Equal(honed, ProgressDomain.IsHoned(scorePercent, retries));
    }

    [Fact]
    public void Honed_retry_boundary_comes_from_the_attempts()
    {
        // Ten cards: nine right first time, one wrong then right = 90% and one retry.
        var nineFirstTry = Enumerable.Range(0, 9)
            .Select(index => new GradedAnswer($"card-{index}", true))
            .ToList();

        var oneRetry = nineFirstTry.Append(new GradedAnswer("card-9", false)).ToList();

        var withOneRetry = ProgressDomain.Evaluate(oneRetry.Append(new GradedAnswer("card-9", true)), []);
        Assert.Equal(90, withOneRetry.ScorePercent);
        Assert.Equal(1, withOneRetry.Retries);
        Assert.True(withOneRetry.Honed);

        var withThreeRetries = ProgressDomain.Evaluate(
            nineFirstTry
                .Concat([new GradedAnswer("card-9", false)])
                .Concat([new GradedAnswer("card-9", false)])
                .Concat([new GradedAnswer("card-9", false)])
                .Concat([new GradedAnswer("card-9", true)]),
            []);

        Assert.Equal(90, withThreeRetries.ScorePercent);
        Assert.Equal(3, withThreeRetries.Retries);
        Assert.False(withThreeRetries.Honed);
    }

    [Fact]
    public void A_session_is_complete_only_when_the_queue_is_empty()
    {
        Assert.True(ProgressDomain.IsComplete([]));
        Assert.False(ProgressDomain.IsComplete(["card-fib-headroom"]));
        Assert.Null(ProgressDomain.Next([]));
        Assert.Equal("card-fib-headroom", ProgressDomain.Next(["card-fib-headroom", "card-fib-whitebalance"]));    }

    [Fact]
    public void A_completed_seventy_five_percent_run_still_grants_the_tool()
    {
        // The four-card deck with one wrong first try: 75% is a fail, but the deck was finished,
        // so the skill's tool is earned. That is deliberate, not a rounding accident.
        GradedAnswer[] answers =
        [
            new("card-1", true),
            new("card-2", true),
            new("card-3", true),
            new("card-4", false),
            new("card-4", true)
        ];

        var outcome = ProgressDomain.Evaluate(answers, []);

        Assert.Equal(3, outcome.FirstTryCorrect);
        Assert.Equal(4, outcome.TotalCards);
        Assert.Equal(75, outcome.ScorePercent);
        Assert.False(outcome.Passed);
        Assert.False(outcome.Honed);
        Assert.True(outcome.Completed);
        Assert.True(outcome.ToolGranted);
    }

    [Fact]
    public void Leaving_the_queue_full_is_not_completed_and_does_not_grant_the_tool()
    {
        var outcome = ProgressDomain.Evaluate(
            [new GradedAnswer("card-1", true)],
            ["card-2", "card-3"]);

        Assert.False(outcome.Completed);
        Assert.False(outcome.ToolGranted);
        Assert.Equal(1, outcome.TotalCards);
    }

    [Fact]
    public void A_plain_run_after_a_honed_one_keeps_the_skill_honed()
    {
        // The later plain session does not erase the earlier honed one, and a deck Content no
        // longer knows cannot invent a skill.
        (string DeckId, bool Honed)[] sessions =
        [
            ("deck-camera-foundations", true),
            ("deck-camera-foundations", false),
            ("deck-retired", true)
        ];

        var skillIds = ProgressDomain.HonedSkillIds(
            sessions,
            new Dictionary<string, string> { ["deck-camera-foundations"] = "framing" });

        Assert.Equal(new[] { "framing" }, skillIds);
    }

    [Fact]
    public void Review_due_comes_back_quickly_when_wrong_and_days_later_when_right()
    {
        var now = new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);

        Assert.Equal(now.AddDays(3), ProgressDomain.DueAt(correct: true, now));
        Assert.Equal(now.AddMinutes(10), ProgressDomain.DueAt(correct: false, now));
    }
}
