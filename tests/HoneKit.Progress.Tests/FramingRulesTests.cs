using HoneKit.Contracts;
using HoneKit.Progress.Domain;

namespace HoneKit.Progress.Tests;

// Framing geometry only: IoU, the 0.70 dial, and the clamp-free answer parser. The endpoint turns
// these results into attempts; nothing here needs a session or a database.
public sealed class FramingRulesTests
{
    // A full-frame target keeps the arithmetic honest: the submitted area is the overlap.
    private static readonly RectDto FullFrame = new(0, 0, 1, 1);

    [Fact]
    public void Identical_frames_overlap_completely()
    {
        var overlap = FramingRules.Overlap(FullFrame, FullFrame);

        Assert.Equal(1.0, overlap);
        Assert.True(FramingRules.IsCorrect(overlap));
    }

    [Fact]
    public void Disjoint_frames_do_not_overlap()
    {
        var overlap = FramingRules.Overlap(new RectDto(0, 0, 0.2, 0.2), new RectDto(0.5, 0.5, 0.2, 0.2));

        Assert.Equal(0.0, overlap);
        Assert.False(FramingRules.IsCorrect(overlap));
    }

    [Fact]
    public void A_frame_inside_the_target_scores_by_area()
    {
        // Half the width and half the height of the target: a quarter of its area.
        var overlap = FramingRules.Overlap(new RectDto(0, 0, 0.5, 0.5), FullFrame);

        Assert.Equal(0.25, overlap, 9);
        Assert.False(FramingRules.IsCorrect(overlap));
    }

    [Fact]
    public void Sixty_nine_percent_of_the_target_is_a_miss()
    {
        var overlap = FramingRules.Overlap(new RectDto(0, 0, 0.69, 1), FullFrame);

        Assert.True(overlap < FramingRules.OverlapThreshold, $"Expected a miss, got {overlap:R}");
        Assert.Equal(0.69, overlap, 9);
        Assert.False(FramingRules.IsCorrect(overlap));
    }

    [Fact]
    public void Seventy_percent_of_the_target_is_a_hit()
    {
        var overlap = FramingRules.Overlap(new RectDto(0, 0, 0.70, 1), FullFrame);

        // The dial is inclusive: exactly on the line counts.
        Assert.True(overlap >= FramingRules.OverlapThreshold, $"Expected a hit, got {overlap:R}");
        Assert.Equal(0.70, overlap, 9);
        Assert.True(FramingRules.IsCorrect(overlap));
    }

    [Fact]
    public void A_normalised_frame_parses_out_of_the_answer_string()
    {
        var parsed = FramingRules.TryParseRect(
            """{"x":0.5,"y":0.16,"width":0.32,"height":0.46}""",
            out var rect,
            out var error);

        Assert.True(parsed, error);
        Assert.Equal(new RectDto(0.5, 0.16, 0.32, 0.46), rect);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("""{"x":"0.5","y":0.1,"width":0.2,"height":0.2}""")]
    public void Malformed_answers_are_rejected(string answer)
    {
        Assert.False(FramingRules.TryParseRect(answer, out var rect, out var error));
        Assert.Null(rect);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Theory]
    [InlineData("""{"x":0.5,"y":0.16,"width":0.32}""")]
    [InlineData("""{"x":0.5,"y":0.16,"width":0.32,"height":null}""")]
    [InlineData("{}")]
    public void Frames_missing_a_field_are_rejected(string answer)
    {
        Assert.False(FramingRules.TryParseRect(answer, out _, out _));
    }

    [Theory]
    [InlineData("""{"x":1.2,"y":0.1,"width":0.2,"height":0.2}""")]
    [InlineData("""{"x":0.1,"y":-0.1,"width":0.2,"height":0.2}""")]
    [InlineData("""{"x":0.1,"y":0.1,"width":-0.2,"height":0.2}""")]
    [InlineData("""{"x":0.1,"y":0.1,"width":0.2,"height":1.5}""")]
    [InlineData("""{"x":0.1,"y":0.1,"width":1e400,"height":0.2}""")]
    public void Out_of_range_frames_are_rejected_not_clamped(string answer)
    {
        Assert.False(FramingRules.TryParseRect(answer, out var rect, out _));
        Assert.Null(rect);
    }
}
