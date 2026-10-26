namespace HoneKit.Requirements.Domain;

/// <summary>One requirement as the document states it, before it is validated.</summary>
public sealed record ParsedRequirementSkill(
    string SkillId,
    double MinLevel,
    int Headcount,
    string Rationale);

/// <summary>
/// The parse contract the model is asked for: purpose, a criticality guess, skills from the closed
/// catalogue, and the services the brief asks for that this role does not cover.
/// </summary>
public sealed class ParsedRequirements
{
    public string Purpose { get; set; } = "";

    public string Criticality { get; set; } = "";

    public List<ParsedRequirementSkill> Skills { get; set; } = [];

    public List<string> OutsideRole { get; set; } = [];
}

public static class RequirementDraft
{
    /// <summary>
    /// Trims what people and models get wrong harmlessly: whitespace, criticality casing, skill-id
    /// casing, blank outside-role entries. It never repairs a wrong value — the validator rejects those.
    /// </summary>
    public static ParsedRequirements Normalize(ParsedRequirements draft) => new()
    {
        Purpose = (draft.Purpose ?? "").Trim(),
        Criticality = (draft.Criticality ?? "").Trim().ToLowerInvariant(),
        Skills = (draft.Skills ?? [])
            .Select(skill => new ParsedRequirementSkill(
                (skill.SkillId ?? "").Trim().ToLowerInvariant(),
                skill.MinLevel,
                skill.Headcount,
                (skill.Rationale ?? "").Trim()))
            .ToList(),
        OutsideRole = (draft.OutsideRole ?? [])
            .Select(service => (service ?? "").Trim())
            .Where(service => service.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
    };
}
