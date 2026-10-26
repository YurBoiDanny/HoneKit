using HoneKit.Contracts;

namespace HoneKit.Requirements.Domain;

/// <summary>
/// The shared rules for a requirement set, whether a model drafted it or a leader typed it. Plain
/// sentences, because they are shown to a person next to a manual form.
/// </summary>
public static class RequirementValidation
{
    public const int MinHeadcount = 1;

    public const int MaxHeadcount = 20;

    public static IReadOnlyList<double> AllowedLevels { get; } = [0, 0.5, 1];

    public static IReadOnlyList<string> Criticalities { get; } = ["low", "medium", "high"];

    public static IReadOnlyList<string> Validate(ParsedRequirements draft, IReadOnlyList<SkillDto> catalogue)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(draft.Purpose))
        {
            errors.Add("Add a purpose for this meeting.");
        }

        if (!Criticalities.Contains(draft.Criticality, StringComparer.OrdinalIgnoreCase))
        {
            errors.Add("Criticality must be low, medium or high.");
        }

        var skillById = catalogue
            .DistinctBy(skill => skill.Id)
            .ToDictionary(skill => skill.Id, StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var requirement in draft.Skills ?? [])
        {
            var skillId = (requirement.SkillId ?? "").Trim();

            if (!skillById.TryGetValue(skillId, out var known))
            {
                errors.Add($"'{skillId}' is not a skill HoneKit knows. Choose one from the catalogue.");
                continue;
            }

            if (!seen.Add(known.Id))
            {
                errors.Add($"{known.Name} is listed twice; keep one requirement per skill.");
            }

            if (!AllowedLevels.Contains(requirement.MinLevel))
            {
                errors.Add($"{known.Name}'s minimum level must be 0, 0.5 or 1.");
            }

            if (requirement.Headcount is < MinHeadcount or > MaxHeadcount)
            {
                errors.Add($"{known.Name}'s headcount must be between {MinHeadcount} and {MaxHeadcount}.");
            }

            if (string.IsNullOrWhiteSpace(requirement.Rationale))
            {
                errors.Add($"Add a short reason for {known.Name}.");
            }
        }

        return errors;
    }
}
