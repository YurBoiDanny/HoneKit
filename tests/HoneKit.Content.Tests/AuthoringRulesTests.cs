using HoneKit.Contracts;
using HoneKit.Content.Domain;

namespace HoneKit.Content.Tests;

// The authoring rules only: no HTTP, no EF. These are the sentences the deck builder shows.
public sealed class AuthoringRulesTests
{
    private static readonly SkillDto[] RoleSkills =
    [
        new("switching", "camera", "Switching", 2)
    ];

    private static readonly LessonDto[] Lessons =
    [
        new("switching-1", "switching", 1, "Preview and program"),
        new("switching-2", "switching", 2, "The multiviewer")
    ];

    private static CreateCardRequest ValidFillInTheBlank(
        string prompt = "Before a cut, the ___ is the camera you will be on when the shot lands.",
        IReadOnlyList<ChoiceDto>? choices = null,
        string? correctChoiceId = "preview",
        IReadOnlyDictionary<string, string>? wrongReasons = null) =>
        new(
            LessonId: "switching-1",
            Type: "fill-in-the-blank",
            ImageRef: "/seed/stage-01.jpg",
            Prompt: prompt,
            Choices: choices ?? [new ChoiceDto("preview", "preview"), new ChoiceDto("program", "program")],
            CorrectChoiceId: correctChoiceId,
            TargetRect: null,
            Explanation: "Preview is the source you are lining up; program is what is live.",
            WrongReasons: wrongReasons ?? new Dictionary<string, string>
            {
                ["program"] = "Program is what is live now, not the shot you are lining up."
            });

    private static CreateCardRequest ValidFraming(RectDto? target = null) =>
        new(
            LessonId: "switching-2",
            Type: "framing",
            ImageRef: "/seed/stage-03.jpg",
            Prompt: "Frame the stage wide enough to read the room.",
            Choices: [],
            CorrectChoiceId: null,
            TargetRect: target ?? new RectDto(0.2, 0.2, 0.4, 0.4),
            Explanation: "Wide enough for the room, tight enough for the stage.",
            WrongReasons: new Dictionary<string, string>());

    [Fact]
    public void A_valid_deck_passes()
    {
        Assert.Empty(AuthoringRules.ValidateDeck("camera", "Switching Essentials", "switching", RoleSkills));
    }

    [Fact]
    public void A_deck_needs_a_name_and_a_skill_from_its_role()
    {
        Assert.Contains(AuthoringRules.ValidateDeck("camera", "  ", "switching", RoleSkills),
            error => error.Contains("name", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(AuthoringRules.ValidateDeck("camera", new string('x', 81), "switching", RoleSkills),
            error => error.Contains("80", StringComparison.Ordinal));

        Assert.Contains(AuthoringRules.ValidateDeck("lighting", "Lighting", "switching", []),
            error => error.Contains("role", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(AuthoringRules.ValidateDeck("camera", "Deck", "audio-1", RoleSkills),
            error => error.Contains("'audio-1'", StringComparison.Ordinal));
    }

    [Fact]
    public void A_valid_fill_in_the_blank_card_passes()
    {
        Assert.Empty(AuthoringRules.ValidateCard(ValidFillInTheBlank(), "switching", Lessons));
    }

    [Fact]
    public void A_fill_in_the_blank_prompt_needs_the_blank()
    {
        var card = ValidFillInTheBlank(prompt: "Before a cut, which camera are you on?");

        var errors = AuthoringRules.ValidateCard(card, "switching", Lessons);

        Assert.Contains(errors, error => error.Contains("___", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void A_fill_in_the_blank_needs_two_to_four_choices(int count)
    {
        var choices = Enumerable.Range(1, count)
            .Select(index => new ChoiceDto($"choice-{index}", $"choice {index}"))
            .ToArray();

        var card = ValidFillInTheBlank(choices: choices, correctChoiceId: "choice-1");

        var errors = AuthoringRules.ValidateCard(card, "switching", Lessons);

        Assert.Contains(errors, error => error.Contains("2 to 4", StringComparison.Ordinal));
    }

    [Fact]
    public void Choice_ids_must_be_unique_and_the_correct_one_listed()
    {
        var duplicate = ValidFillInTheBlank(
            choices: [new ChoiceDto("preview", "preview"), new ChoiceDto("preview", "program")]);

        Assert.Contains(AuthoringRules.ValidateCard(duplicate, "switching", Lessons),
            error => error.Contains("unique", StringComparison.Ordinal));

        var missingCorrect = ValidFillInTheBlank(correctChoiceId: "tally");

        Assert.Contains(AuthoringRules.ValidateCard(missingCorrect, "switching", Lessons),
            error => error.Contains("one of the choices", StringComparison.Ordinal));

        var emptyId = ValidFillInTheBlank(
            choices: [new ChoiceDto("", "preview"), new ChoiceDto("program", "program")]);

        Assert.Contains(AuthoringRules.ValidateCard(emptyId, "switching", Lessons),
            error => error.Contains("Every choice needs an id", StringComparison.Ordinal));
    }

    [Fact]
    public void Wrong_reasons_must_belong_to_a_wrong_choice()
    {
        var unknown = ValidFillInTheBlank(wrongReasons: new Dictionary<string, string>
        {
            ["tally"] = "A tally light marks the live camera."
        });

        Assert.Contains(AuthoringRules.ValidateCard(unknown, "switching", Lessons),
            error => error.Contains("must belong to one of the choices", StringComparison.Ordinal));

        var forCorrect = ValidFillInTheBlank(wrongReasons: new Dictionary<string, string>
        {
            ["preview"] = "This is the right answer."
        });

        Assert.Contains(AuthoringRules.ValidateCard(forCorrect, "switching", Lessons),
            error => error.Contains("does not need a wrong reason", StringComparison.Ordinal));
    }

    [Fact]
    public void A_valid_framing_card_passes()
    {
        Assert.Empty(AuthoringRules.ValidateCard(ValidFraming(), "switching", Lessons));
    }

    [Fact]
    public void A_framing_target_must_sit_inside_the_image()
    {
        Assert.Contains(AuthoringRules.ValidateCard(ValidFraming(new RectDto(0.7, 0.2, 0.4, 0.2)), "switching", Lessons),
            error => error.Contains("inside the image", StringComparison.Ordinal));

        Assert.Contains(AuthoringRules.ValidateCard(ValidFraming(new RectDto(0.2, 0.2, 0, 0.2)), "switching", Lessons),
            error => error.Contains("inside the image", StringComparison.Ordinal));

        Assert.Contains(AuthoringRules.ValidateCard(ValidFraming(new RectDto(-0.1, 0.2, 0.2, 0.2)), "switching", Lessons),
            error => error.Contains("inside the image", StringComparison.Ordinal));

        var missing = ValidFraming() with { TargetRect = null };

        Assert.Contains(AuthoringRules.ValidateCard(missing, "switching", Lessons),
            error => error.Contains("Draw the target frame", StringComparison.Ordinal));
    }

    [Fact]
    public void The_seeded_card_shapes_pass()
    {
        LessonDto[] framingLessons =
        [
            new("framing-1", "framing", 1, "Reading the frame"),
            new("framing-2", "framing", 2, "Light and colour")
        ];

        var headroom = new CreateCardRequest(
            LessonId: "framing-1",
            Type: "fill-in-the-blank",
            ImageRef: "/seed/stage-03.jpg",
            Prompt: "The space above the speaker's head is called ___. Leave too much and they look lost in the room.",
            Choices:
            [
                new ChoiceDto("headroom", "headroom"),
                new ChoiceDto("lead-room", "lead room"),
                new ChoiceDto("backlight", "backlight"),
                new ChoiceDto("depth", "depth")
            ],
            CorrectChoiceId: "headroom",
            TargetRect: null,
            Explanation: "Headroom is the gap between the top of the frame and the speaker's head.",
            WrongReasons: new Dictionary<string, string>
            {
                ["lead-room"] = "Lead room is the space in front of someone as they move.",
                ["backlight"] = "Backlight is light behind the subject.",
                ["depth"] = "Depth is how much of the scene is in focus."
            });

        var lectern = new CreateCardRequest(
            LessonId: "framing-1",
            Type: "framing",
            ImageRef: "/seed/stage-01.jpg",
            Prompt: "Frame the leader teaching at the lectern.",
            Choices: [],
            CorrectChoiceId: null,
            TargetRect: new RectDto(0.50, 0.16, 0.32, 0.46),
            Explanation: "The lectern sits right of centre.",
            WrongReasons: new Dictionary<string, string>());

        Assert.Empty(AuthoringRules.ValidateCard(headroom, "framing", framingLessons));
        Assert.Empty(AuthoringRules.ValidateCard(lectern, "framing", framingLessons));
    }
}
