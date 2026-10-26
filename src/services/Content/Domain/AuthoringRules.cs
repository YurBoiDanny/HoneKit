using HoneKit.Contracts;

namespace HoneKit.Content.Domain;

/// <summary>
/// The authoring rules, with no EF and no HTTP. A leader gets plain sentences back; nothing is
/// silently repaired or defaulted, because the editor should show what is missing.
/// </summary>
public static class AuthoringRules
{
    public const int MaxDeckNameLength = 80;

    public const int MinChoices = 2;

    public const int MaxChoices = 4;

    public const string FillInTheBlank = "fill-in-the-blank";

    public const string Framing = "framing";

    public static IReadOnlyList<string> ValidateDeck(
        string? role,
        string? name,
        string? skillId,
        IReadOnlyList<SkillDto> roleSkills)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Give the deck a name.");
        }
        else if (name.Trim().Length > MaxDeckNameLength)
        {
            errors.Add($"Keep the deck name to {MaxDeckNameLength} characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(role) || roleSkills.Count == 0)
        {
            errors.Add("Choose a role HoneKit has skills for.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(skillId))
        {
            errors.Add("Choose the skill this deck teaches.");
        }
        else if (roleSkills.All(skill => skill.Id != skillId.Trim()))
        {
            errors.Add($"'{skillId.Trim()}' is not one of the {role.Trim()} skills.");
        }

        return errors;
    }

    public static IReadOnlyList<string> ValidateCard(
        CreateCardRequest request,
        string deckSkillId,
        IReadOnlyList<LessonDto> lessons)
    {
        var errors = new List<string>();

        var lesson = lessons.FirstOrDefault(candidate => candidate.Id == request.LessonId);
        if (lesson is null)
        {
            errors.Add("That lesson does not exist.");
        }
        else if (lesson.SkillId != deckSkillId)
        {
            errors.Add("That lesson is not part of this deck's skill.");
        }

        var type = (request.Type ?? "").Trim();

        if (type is not (FillInTheBlank or Framing))
        {
            errors.Add("Cards are either fill-in-the-blank or framing.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            errors.Add("Write the card's prompt.");
        }

        if (string.IsNullOrWhiteSpace(request.Explanation))
        {
            errors.Add("Add an explanation of the right answer.");
        }

        if (string.IsNullOrWhiteSpace(request.ImageRef))
        {
            errors.Add("Add an image for the card.");
        }

        if (type == FillInTheBlank)
        {
            ValidateFillInTheBlank(request, errors);
        }
        else
        {
            ValidateFraming(request, errors);
        }

        return errors;
    }

    private static void ValidateFillInTheBlank(CreateCardRequest request, List<string> errors)
    {
        var choices = request.Choices ?? [];

        if (choices.Count is < MinChoices or > MaxChoices)
        {
            errors.Add($"A fill-in-the-blank card needs {MinChoices} to {MaxChoices} choices.");
        }

        if (choices.Any(choice => string.IsNullOrWhiteSpace(choice.Id)))
        {
            errors.Add("Every choice needs an id.");
        }

        var ids = choices
            .Select(choice => choice.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToArray();

        if (ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            errors.Add("Choice ids must be unique.");
        }

        if (choices.Any(choice => string.IsNullOrWhiteSpace(choice.Label)))
        {
            errors.Add("Every choice needs a label.");
        }

        if (string.IsNullOrWhiteSpace(request.CorrectChoiceId))
        {
            errors.Add("Mark which choice is correct.");
        }
        else if (!ids.Contains(request.CorrectChoiceId, StringComparer.Ordinal))
        {
            errors.Add("The correct choice must be one of the choices.");
        }

        if (!string.IsNullOrWhiteSpace(request.Prompt)
            && !request.Prompt.Contains("___", StringComparison.Ordinal))
        {
            errors.Add("A fill-in-the-blank prompt needs ___ where the answer goes.");
        }

        foreach (var (choiceId, _) in request.WrongReasons ?? new Dictionary<string, string>())
        {
            if (!ids.Contains(choiceId, StringComparer.Ordinal))
            {
                errors.Add($"The reason for '{choiceId}' must belong to one of the choices.");
            }
            else if (choiceId == request.CorrectChoiceId)
            {
                errors.Add("The correct choice does not need a wrong reason.");
            }
        }
    }

    private static void ValidateFraming(CreateCardRequest request, List<string> errors)
    {
        if (request.TargetRect is null)
        {
            errors.Add("Draw the target frame for this card.");
        }
        else if (!IsInsideImage(request.TargetRect))
        {
            errors.Add("The target frame must sit inside the image.");
        }

        if ((request.Choices?.Count ?? 0) > 0)
        {
            errors.Add("A framing card does not have choices.");
        }

        if (!string.IsNullOrWhiteSpace(request.CorrectChoiceId))
        {
            errors.Add("A framing card does not have a correct choice.");
        }
    }

    private static bool IsInsideImage(RectDto rect) =>
        double.IsFinite(rect.X)
        && double.IsFinite(rect.Y)
        && double.IsFinite(rect.Width)
        && double.IsFinite(rect.Height)
        && rect.X >= 0
        && rect.Y >= 0
        && rect.Width > 0
        && rect.Height > 0
        && rect.X + rect.Width <= 1
        && rect.Y + rect.Height <= 1;
}
