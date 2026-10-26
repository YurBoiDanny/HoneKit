namespace HoneKit.Roster.Domain;

/// <summary>
/// The bag shapes the frontend offers. Values are matched exactly: a typo is rejected rather than
/// silently repainting someone's bag, and the stored value is the token the UI already knows.
/// </summary>
public static class BagStyleRules
{
    public static IReadOnlyList<string> Colours { get; } = ["sage", "rose", "powder", "butter", "brass", "teal"];

    public static IReadOnlyList<string> Styles { get; } = ["backpack", "duffel", "sling"];

    public static bool IsValidColour(string? colour) => colour is not null && Colours.Contains(colour, StringComparer.Ordinal);

    public static bool IsValidStyle(string? style) => style is not null && Styles.Contains(style, StringComparer.Ordinal);

    public static bool TryValidate(string? colour, string? style, out string error)
    {
        if (!IsValidColour(colour))
        {
            error = $"Colour must be one of: {string.Join(", ", Colours)}.";
            return false;
        }

        if (!IsValidStyle(style))
        {
            error = $"Style must be one of: {string.Join(", ", Styles)}.";
            return false;
        }

        error = "";
        return true;
    }
}
