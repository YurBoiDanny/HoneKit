using HoneKit.Roster.Domain;

namespace HoneKit.Roster.Tests;

// The two closed vocabularies the personalization UI offers. Exact tokens, no defaults: an unknown
// value is a bad request, not a quiet repaint.
public sealed class BagStyleRulesTests
{
    [Fact]
    public void Every_offered_colour_and_style_is_valid()
    {
        foreach (var colour in BagStyleRules.Colours)
        {
            foreach (var style in BagStyleRules.Styles)
            {
                Assert.True(BagStyleRules.TryValidate(colour, style, out var error), error);
                Assert.Equal("", error);
            }
        }
    }

    [Fact]
    public void An_unknown_colour_is_rejected()
    {
        Assert.False(BagStyleRules.TryValidate("mustard", "backpack", out var error));
        Assert.Contains("Colour", error);
    }

    [Fact]
    public void An_unknown_style_is_rejected()
    {
        Assert.False(BagStyleRules.TryValidate("rose", "tote", out var error));
        Assert.Contains("Style", error);
    }

    [Fact]
    public void Validation_is_case_sensitive()
    {
        Assert.False(BagStyleRules.IsValidColour("Sage"));
        Assert.False(BagStyleRules.IsValidStyle("Backpack"));
        Assert.False(BagStyleRules.TryValidate("Rose", "duffel", out _));
    }
}
