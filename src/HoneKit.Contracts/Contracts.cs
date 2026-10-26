namespace HoneKit.Contracts;

// The one definition every service and the frontend build against.
// Property names go over the wire in camelCase (ASP.NET Core's default JSON policy);
// tools/ContractGen turns these records into src/frontend/src/api/types.ts.

public sealed record SkillDto(string Id, string Role, string Name, int LessonCount);

public sealed record LessonDto(string Id, string SkillId, int Ordinal, string Title);

public sealed record DeckSummaryDto(string Id, string Role, string Name, string SkillId, int CardCount);

public sealed record ChoiceDto(string Id, string Label);

public sealed record RectDto(double X, double Y, double Width, double Height);

public sealed record CardDto(
    string Id,
    string DeckId,
    string LessonId,
    string Type,
    string? ImageRef,
    string Prompt,
    IReadOnlyList<ChoiceDto> Choices,
    RectDto? TargetRect,
    string Explanation,
    IReadOnlyDictionary<string, string> WrongReasons);

public sealed record MemberDto(
    string Id,
    string Name,
    string PrimaryRole,
    string? SecondaryRole,
    string Colour,
    string Style,
    bool Worn,
    DateTimeOffset? LastActiveAt);

public sealed record TeamDto(string Role, string Name, IReadOnlyList<MemberDto> Members);

public sealed record EarnedToolDto(string SkillId, string Name, bool Honed, bool Acknowledged);

public sealed record BagDto(
    string MemberId,
    string Name,
    string Colour,
    string Style,
    bool Worn,
    IReadOnlyList<EarnedToolDto> Tools,
    bool ShowAnswerOnWrong);

public sealed record CompletionsDto(string MemberId, IReadOnlyList<string> LessonIds);

public sealed record PreferencesRequest(bool ShowAnswerOnWrong);

public sealed record HonedSkillsDto(string MemberId, IReadOnlyList<string> SkillIds);

public sealed record AcknowledgeRequest(string SkillId);

public sealed record BagStyleRequest(string Colour, string Style);

public sealed record BagWallMemberDto(
    string Id,
    string Name,
    string Colour,
    string Style,
    bool Worn,
    IReadOnlyList<string> Tools,
    DateTimeOffset? LastActiveAt);

public sealed record BagWallDto(string Role, string Name, IReadOnlyList<BagWallMemberDto> Members);

public sealed record TeamToolDto(
    string SkillId,
    string Name,
    int FilledLessons,
    int TotalLessons,
    double Percent);

public sealed record TeamBagDto(
    string Role,
    string Name,
    int MemberCount,
    IReadOnlyList<TeamToolDto> Tools,
    string Wear);

// ---- the member loop -------------------------------------------------------------------

public sealed record StartSessionRequest(string MemberId, string DeckId);

public sealed record SessionDto(string Id, string MemberId, string DeckId, int TotalCards, int Remaining);

public sealed record NextCardDto(string? CardId, int Remaining);

public sealed record AttemptRequest(string CardId, string Answer);

// Grading data, fetched by Progress. Kept off CardDto so the card payload the browser
// receives does not carry the answer.
public sealed record CardAnswerDto(
    string CardId,
    string? CorrectChoiceId,
    string Explanation,
    IReadOnlyDictionary<string, string> WrongReasons,
    RectDto? TargetRect = null);

public sealed record AttemptResultDto(
    bool Correct,
    string? CorrectChoiceId,
    string? Explanation,
    string? WrongReason,
    int? RequeuedAfter,
    DateTimeOffset DueAt,
    int Remaining,
    double? Overlap = null);

public sealed record FinishSessionRequest(bool ExitedEarly);

public sealed record SessionResultDto(
    string SessionId,
    int FirstTryCorrect,
    int TotalCards,
    double ScorePercent,
    bool Passed,
    bool Honed,
    bool ToolGranted,
    string SkillId,
    string SkillName);
