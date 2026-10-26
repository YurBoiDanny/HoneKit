using System.Text.Json;
using HoneKit.Contracts;

namespace HoneKit.Progress.Domain;

/// <summary>
/// Sequence grading for ordering cards (step ids in order) and ptz cards (Bolin actions in order),
/// plus the sentences the reveal shows. Pure: no HTTP, no EF.
/// </summary>
public static class SequenceRules
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// The answer is a JSON array of strings. Malformed input is a bad request, never a quiet miss.
    /// </summary>
    public static bool TryParseAnswer(string? json, out IReadOnlyList<string> values, out string error)
    {
        values = [];

        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Send the answer as a JSON array.";
            return false;
        }

        string?[]? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<string?[]>(json, JsonOptions);
        }
        catch (JsonException)
        {
            error = "That answer is not a JSON array of strings.";
            return false;
        }

        if (parsed is null)
        {
            error = "That answer is not a JSON array of strings.";
            return false;
        }

        if (parsed.Any(value => string.IsNullOrWhiteSpace(value)))
        {
            error = "Every answer entry must be a non-empty string.";
            return false;
        }

        values = parsed.Select(value => value!.Trim()).ToArray();
        error = "";
        return true;
    }

    /// <summary>Same entries, same order. The comparer decides how strict the ids or actions are.</summary>
    public static bool MatchesExpected(
        IReadOnlyList<string> submitted,
        IReadOnlyList<string> expected,
        StringComparer comparer) =>
        submitted.Count == expected.Count
        && submitted.Zip(expected, (left, right) => comparer.Equals(left, right)).All(match => match);

    /// <summary>The right order as a numbered sentence: "1. …, 2. …".</summary>
    public static string DescribeOrder(IReadOnlyList<OrderingStepDto> steps, IReadOnlyList<string> correctOrder)
    {
        var labels = steps
            .DistinctBy(step => step.Id)
            .ToDictionary(step => step.Id, step => step.Label, StringComparer.Ordinal);

        return string.Join(", ", correctOrder.Select((id, index) =>
            $"{index + 1}. {(labels.TryGetValue(id, out var label) ? label : id)}"));
    }

    /// <summary>The right actions as a readable sentence: "Camera 3 to preset 10."</summary>
    public static string DescribeActions(IReadOnlyList<string> correctActions)
    {
        if (correctActions.Count == 0)
        {
            return "No actions.";
        }

        var sentences = correctActions.Select((action, index) =>
        {
            var sentence = DescribeAction(action);
            return index == 0 ? sentence : char.ToLowerInvariant(sentence[0]) + sentence[1..];
        });

        return string.Join(", then ", sentences) + ".";
    }

    private static string DescribeAction(string action)
    {
        var parts = action.Split(':', StringSplitOptions.TrimEntries);

        if (parts.Length == 3 && parts[0].StartsWith("cam", StringComparison.OrdinalIgnoreCase))
        {
            return $"Camera {parts[0][3..]} to {parts[1].ToLowerInvariant()} {parts[2]}";
        }

        return action;
    }
}
