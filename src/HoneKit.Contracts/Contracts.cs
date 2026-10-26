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

// ---- meeting requirements ----------------------------------------------------------------

public sealed record RequirementSkillInputDto(
    string SkillId,
    double MinLevel,
    int Headcount,
    string Rationale);

public sealed record RequirementSkillDto(
    string SkillId,
    string Name,
    double MinLevel,
    int Headcount,
    string Rationale);

public sealed record RequirementSetDto(
    string Id,
    string SourceName,
    string SourceText,
    string Purpose,
    string Criticality,
    string Provider,
    string Model,
    DateTimeOffset ParsedAt,
    DateTimeOffset? ConfirmedAt,
    bool Edited,
    IReadOnlyList<RequirementSkillDto> Skills,
    IReadOnlyList<string> OutsideRole);

public sealed record RequirementSummaryDto(
    string Id,
    string SourceName,
    string Purpose,
    string Provider,
    DateTimeOffset ParsedAt,
    DateTimeOffset? ConfirmedAt);

public sealed record UpdateRequirementSetRequest(
    string Purpose,
    string Criticality,
    IReadOnlyList<RequirementSkillInputDto> Skills,
    IReadOnlyList<string> OutsideRole);

public sealed record ManualRequirementSetRequest(
    string Purpose,
    string Criticality,
    IReadOnlyList<RequirementSkillInputDto> Skills,
    IReadOnlyList<string> OutsideRole,
    string? SourceName,
    string? SourceText);

// Returned when a brief cannot be parsed. sourceText lets the leader keep their document
// and type the requirements instead of uploading it again.
public sealed record ParseFailureDto(
    string Detail,
    string SourceName,
    string SourceText,
    IReadOnlyList<string> Errors);

public sealed record AiStatusDto(
    string Provider,
    string Model,
    string Endpoint,
    bool KeyConfigured,
    string Mode,
    DateTimeOffset? LastParseAt,
    bool? LastParseOk,
    string Detail);

// ---- recommendations --------------------------------------------------------------------

public sealed record SkillProgressDto(
    string SkillId,
    string Name,
    int CompletedLessons,
    int TotalLessons,
    double Percent,
    DateTimeOffset? LastAnsweredAt);

public sealed record MemberProgressDto(string MemberId, IReadOnlyList<SkillProgressDto> Skills);

public sealed record RecommendationRequest(string RequirementId, string Strategy);

public sealed record RecommendedMemberDto(
    string MemberId,
    string Name,
    string Colour,
    string Style,
    bool Worn,
    double Readiness,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Warnings);

public sealed record RecommendationGapDto(
    string SkillId,
    string Name,
    string? DeckId,
    int Shortfall,
    IReadOnlyList<string> SuggestedMemberIds);

public sealed record AssignDeckRequest(IReadOnlyList<string> MemberIds);

public sealed record SustainDto(string MemberId, string Name, string Reason);

public sealed record RecommendationDto(
    string RequirementId,
    string Strategy,
    double Coverage,
    IReadOnlyList<RecommendedMemberDto> Suggestions,
    IReadOnlyList<RecommendationGapDto> Gaps,
    IReadOnlyList<SustainDto> Sustain);

public sealed record OverrideCheckRequest(string RequirementId, IReadOnlyList<string> MemberIds);

public sealed record OverrideWarningDto(string MemberId, string Name, IReadOnlyList<string> Warnings);

public sealed record SavePlanRequest(
    string RequirementId,
    string Strategy,
    IReadOnlyList<string> MemberIds);

public sealed record PlanMemberDto(
    string MemberId,
    string Name,
    bool AtLevel,
    IReadOnlyList<string> Warnings);

public sealed record PlanDto(
    string Id,
    string RequirementId,
    string Strategy,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PlanMemberDto> Members,
    IReadOnlyList<RecommendationGapDto> Gaps);

public sealed record PlanSummaryDto(
    string Id,
    string RequirementId,
    string Purpose,
    string Strategy,
    DateTimeOffset CreatedAt,
    int MemberCount);

// ---- authoring --------------------------------------------------------------------------

public sealed record CreateDeckRequest(string Role, string Name, string SkillId);

public sealed record CreateCardRequest(
    string LessonId,
    string Type,
    string? ImageRef,
    string Prompt,
    IReadOnlyList<ChoiceDto> Choices,
    string? CorrectChoiceId,
    RectDto? TargetRect,
    string Explanation,
    IReadOnlyDictionary<string, string> WrongReasons);

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
