using HoneKit.Contracts;
using HoneKit.Progress.Domain;

namespace HoneKit.Progress.Tests;

// Sequence grading for ordering and ptz cards, plus the reveal sentences.
public sealed class SequenceRulesTests
{
    private static readonly OrderingStepDto[] Steps =
    [
        new("connect-ethernet", "Connect ethernet to the Bolin controllers"),
        new("power-on", "Turn the controllers on"),
        new("check-presets", "Check presets 1–13 are framed and exposed"),
        new("check-traces", "Check traces 1–4 play as intended")
    ];

    private static readonly string[] CorrectOrder =
    [
        "connect-ethernet",
        "power-on",
        "check-presets",
        "check-traces"
    ];

    private static bool MatchesOrder(IReadOnlyList<string> submitted) =>
        SequenceRules.MatchesExpected(submitted, CorrectOrder, StringComparer.Ordinal);

    [Fact]
    public void The_exact_order_is_correct()
    {
        Assert.True(MatchesOrder(CorrectOrder));
    }

    [Fact]
    public void A_swapped_pair_is_wrong()
    {
        string[] swapped =
        [
            "power-on",
            "connect-ethernet",
            "check-presets",
            "check-traces"
        ];

        Assert.False(MatchesOrder(swapped));
    }

    [Fact]
    public void An_extra_or_missing_step_is_wrong()
    {
        Assert.False(MatchesOrder([.. CorrectOrder, "check-traces"]));
        Assert.False(MatchesOrder(["connect-ethernet", "power-on", "check-presets"]));
    }

    [Fact]
    public void The_ptz_sequence_matches_exactly()
    {
        Assert.True(SequenceRules.MatchesExpected(["cam3:preset:10"], ["cam3:preset:10"], StringComparer.OrdinalIgnoreCase));
        Assert.True(SequenceRules.MatchesExpected(["CAM3:PRESET:10"], ["cam3:preset:10"], StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void An_extra_or_wrong_ptz_action_is_wrong()
    {
        Assert.False(SequenceRules.MatchesExpected(
            ["cam3:preset:10", "cam1:trace:1"],
            ["cam3:preset:10"],
            StringComparer.OrdinalIgnoreCase));

        Assert.False(SequenceRules.MatchesExpected(
            ["cam3:preset:1"],
            ["cam3:preset:10"],
            StringComparer.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("\"cam3:preset:10\"")]
    [InlineData("[1,2]")]
    [InlineData("[null]")]
    [InlineData("[\"cam3:preset:10\",\"\"]")]
    public void Malformed_answers_are_rejected(string answer)
    {
        Assert.False(SequenceRules.TryParseAnswer(answer, out var values, out var error));
        Assert.Empty(values);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void A_well_formed_array_parses()
    {
        Assert.True(SequenceRules.TryParseAnswer("""["connect-ethernet","power-on"]""", out var values, out _));
        Assert.Equal(["connect-ethernet", "power-on"], values);
    }

    [Fact]
    public void The_reveal_sentences_read_plainly()
    {
        var order = SequenceRules.DescribeOrder(Steps, CorrectOrder);

        Assert.Equal(
            "1. Connect ethernet to the Bolin controllers, 2. Turn the controllers on, 3. Check presets 1–13 are framed and exposed, 4. Check traces 1–4 play as intended",
            order);

        Assert.Equal("Camera 3 to preset 10.", SequenceRules.DescribeActions(["cam3:preset:10"]));
        Assert.Equal(
            "Camera 1 to preset 1, then camera 2 to trace 4.",
            SequenceRules.DescribeActions(["cam1:preset:1", "cam2:trace:4"]));
    }
}
