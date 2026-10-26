using System.Text.Json;
using HoneKit.Contracts;

namespace HoneKit.Progress.Domain;

/// <summary>
/// Framing grading, with no HTTP and no EF. A submitted frame is a normalised 0..1 rectangle; the
/// answer is right when its intersection over union with the author's target frame clears the dial.
/// </summary>
public static class FramingRules
{
    /// <summary>"Close enough is right": IoU of 0.70 or better counts.</summary>
    public const double OverlapThreshold = 0.70;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        // Web defaults read numbers from strings; a frame is numbers, so keep that strict.
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict
    };

    /// <summary>Intersection over union, 0 when the frames do not touch or have no area.</summary>
    public static double Overlap(RectDto submitted, RectDto target)
    {
        var left = Math.Max(submitted.X, target.X);
        var top = Math.Max(submitted.Y, target.Y);
        var right = Math.Min(submitted.X + submitted.Width, target.X + target.Width);
        var bottom = Math.Min(submitted.Y + submitted.Height, target.Y + target.Height);

        var intersection = Math.Max(0, right - left) * Math.Max(0, bottom - top);
        if (intersection <= 0)
        {
            return 0;
        }

        var union = (submitted.Width * submitted.Height) + (target.Width * target.Height) - intersection;

        if (union <= 0)
        {
            return 0;
        }

        // IoU is 0..1 by definition; floating point can land a hair above 1 for identical frames.
        return Math.Min(1, intersection / union);
    }

    public static bool IsCorrect(double overlap) => overlap >= OverlapThreshold;

    /// <summary>Every value must be finite and inside 0..1. Nothing is clamped into range.</summary>
    public static bool IsNormalised(RectDto rect) =>
        IsNormalisedValue(rect.X)
        && IsNormalisedValue(rect.Y)
        && IsNormalisedValue(rect.Width)
        && IsNormalisedValue(rect.Height);

    /// <summary>
    /// Parses the answer string of a framing card. Missing fields, malformed JSON, non-finite
    /// numbers and values outside 0..1 are all rejected rather than repaired.
    /// </summary>
    public static bool TryParseRect(string? json, out RectDto? rect, out string error)
    {
        rect = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Send the frame as JSON with x, y, width and height between 0 and 1.";
            return false;
        }

        RectInput? input;
        try
        {
            input = JsonSerializer.Deserialize<RectInput>(json, JsonOptions);
        }
        catch (JsonException)
        {
            error = "That frame is not valid JSON.";
            return false;
        }

        if (input is null)
        {
            error = "That frame is not valid JSON.";
            return false;
        }

        if (input.X is null || input.Y is null || input.Width is null || input.Height is null)
        {
            error = "The frame needs x, y, width and height.";
            return false;
        }

        var candidate = new RectDto(input.X.Value, input.Y.Value, input.Width.Value, input.Height.Value);

        if (!IsNormalised(candidate))
        {
            error = "The frame's values must be between 0 and 1.";
            return false;
        }

        rect = candidate;
        error = "";
        return true;
    }

    private static bool IsNormalisedValue(double value) => double.IsFinite(value) && value is >= 0 and <= 1;

    // Nullable so a missing field is distinguishable from a legitimate 0.
    private sealed record RectInput(double? X, double? Y, double? Width, double? Height);
}
