using HoneKit.Contracts;
using HoneKit.Requirements.Clients;
using HoneKit.Requirements.Data;
using HoneKit.Requirements.Domain;
using HoneKit.Requirements.Parsing;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HoneKit.Requirements.Endpoints;

public static class RequirementEndpoints
{
    // This POC has one role; requirements are always camera requirements.
    private const string Role = "camera";

    public static IEndpointRouteBuilder MapRequirementEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/requirements");

        // Upload: a file or pasted text. Any failure carries the source text back so the leader can
        // keep their document and type the requirements instead.
        api.MapPost("", ReadBriefAsync)
            .DisableAntiforgery();

        api.MapPost("/manual", AddManualAsync);

        api.MapGet("", async Task<Ok<RequirementSummaryDto[]>> (RequirementsDbContext db, CancellationToken ct) =>
        {
            // SQLite cannot ORDER BY a DateTimeOffset, and there are only a handful of sets.
            var sets = await db.RequirementSets.AsNoTracking().ToListAsync(ct);

            var summaries = sets
                .OrderByDescending(set => set.ParsedAt)
                .ThenByDescending(set => set.Id, StringComparer.Ordinal)
                .Select(set => new RequirementSummaryDto(
                    set.Id,
                    set.SourceName,
                    set.Purpose,
                    set.Provider,
                    set.ParsedAt,
                    set.ConfirmedAt))
                .ToArray();

            return TypedResults.Ok(summaries);
        });

        api.MapGet("/{id}", async Task<Results<Ok<RequirementSetDto>, NotFound, ProblemHttpResult>> (
            string id,
            RequirementsDbContext db,
            ContentClient content,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var set = await LoadAsync(db, id, ct);
            if (set is null)
            {
                return TypedResults.NotFound();
            }

            SkillDto[] catalogue;
            try
            {
                catalogue = await content.GetSkillsAsync(Role, ct);
            }
            catch (Exception exception)
            {
                return ContentUnavailable(exception, loggerFactory, "loading a requirement set");
            }

            return TypedResults.Ok(ToDto(set, catalogue));
        });

        api.MapPut("/{id}", async Task<Results<Ok<RequirementSetDto>, NotFound, JsonHttpResult<ParseFailureDto>, ProblemHttpResult>> (
            string id,
            UpdateRequirementSetRequest request,
            RequirementsDbContext db,
            ContentClient content,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var set = await LoadAsync(db, id, ct);
            if (set is null)
            {
                return TypedResults.NotFound();
            }

            if (set.ConfirmedAt is not null)
            {
                return TypedResults.Problem(
                    title: "This requirement set is confirmed",
                    detail: "Confirmed requirements are locked. Start a new set to change them.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            SkillDto[] catalogue;
            try
            {
                catalogue = await content.GetSkillsAsync(Role, ct);
            }
            catch (Exception exception)
            {
                return ContentUnavailable(exception, loggerFactory, "saving a requirement set");
            }

            var draft = RequirementDraft.Normalize(ToDraft(request));
            var errors = RequirementValidation.Validate(draft, catalogue);

            if (errors.Count > 0)
            {
                return Failure(
                    StatusCodes.Status400BadRequest,
                    "These requirements need a fix before they can be saved.",
                    set.SourceName,
                    set.SourceText,
                    errors);
            }

            set.Purpose = draft.Purpose;
            set.Criticality = draft.Criticality;
            set.OutsideRoleJson = RequirementsJson.Write(draft.OutsideRole);
            set.Edited = true;

            db.RequirementSkills.RemoveRange(set.Skills);
            set.Skills.Clear();

            foreach (var skill in draft.Skills)
            {
                set.Skills.Add(new RequirementSkill
                {
                    Id = $"rs-{Guid.NewGuid():N}",
                    SetId = set.Id,
                    SkillId = skill.SkillId,
                    MinLevel = skill.MinLevel,
                    Headcount = skill.Headcount,
                    Rationale = skill.Rationale
                });
            }

            await db.SaveChangesAsync(ct);

            return TypedResults.Ok(ToDto(set, catalogue));
        });

        api.MapPost("/{id}/confirm", async Task<Results<Ok<RequirementSetDto>, NotFound, ProblemHttpResult>> (
            string id,
            RequirementsDbContext db,
            ContentClient content,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var set = await LoadAsync(db, id, ct);
            if (set is null)
            {
                return TypedResults.NotFound();
            }

            SkillDto[] catalogue;
            try
            {
                catalogue = await content.GetSkillsAsync(Role, ct);
            }
            catch (Exception exception)
            {
                return ContentUnavailable(exception, loggerFactory, "confirming a requirement set");
            }

            if (set.ConfirmedAt is null)
            {
                set.ConfirmedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
            }

            return TypedResults.Ok(ToDto(set, catalogue));
        });

        return app;
    }

    private static async Task<Results<Created<RequirementSetDto>, ProblemHttpResult, JsonHttpResult<ParseFailureDto>>> ReadBriefAsync(
        [FromForm] IFormFile? file,
        [FromForm] string? text,
        RequirementsDbContext db,
        ContentClient content,
        RequirementParser parser,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger("Requirements.Upload");

        var sourceName = file is { Length: > 0 } ? file.FileName : "Pasted text";
        string sourceText;

        if (file is { Length: > 0 })
        {
            var extraction = await BriefTextExtractor.ExtractAsync(file, ct);

            if (!extraction.Succeeded)
            {
                logger.LogInformation("Could not read {SourceName}: {Status}", sourceName, extraction.StatusCode);

                return extraction.StatusCode == StatusCodes.Status422UnprocessableEntity
                    ? Failure(StatusCodes.Status422UnprocessableEntity, extraction.Detail, sourceName, "", [extraction.Detail])
                    : TypedResults.Problem(
                        title: "That file cannot be read",
                        detail: extraction.Detail,
                        statusCode: extraction.StatusCode);
            }

            sourceText = extraction.Text;
        }
        else if (!string.IsNullOrWhiteSpace(text))
        {
            sourceText = text.Trim();
        }
        else
        {
            return TypedResults.Problem(
                title: "No brief supplied",
                detail: "Attach a .pdf, .txt or .md file, or paste the text.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        SkillDto[] catalogue;
        try
        {
            catalogue = await content.GetSkillsAsync(Role, ct);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Content is unavailable while reading a brief");

            return Failure(
                StatusCodes.Status503ServiceUnavailable,
                "The skill catalogue is unavailable, so the brief cannot be read. Type the requirements instead.",
                sourceName,
                sourceText,
                ["Content did not respond."]);
        }

        var parsed = await parser.ParseAsync(sourceName, sourceText, catalogue, ct);

        if (parsed.Failure is not null)
        {
            return TypedResults.Json(parsed.Failure.Body, statusCode: parsed.Failure.StatusCode);
        }

        var draft = parsed.Draft!;
        var set = NewSet(draft, sourceName, sourceText, parsed.Provider, parsed.Model, DateTimeOffset.UtcNow);

        db.RequirementSets.Add(set);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/requirements/{set.Id}", ToDto(set, catalogue));
    }

    private static async Task<Results<Created<RequirementSetDto>, JsonHttpResult<ParseFailureDto>, ProblemHttpResult>> AddManualAsync(
        ManualRequirementSetRequest request,
        RequirementsDbContext db,
        ContentClient content,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        SkillDto[] catalogue;
        try
        {
            catalogue = await content.GetSkillsAsync(Role, ct);
        }
        catch (Exception exception)
        {
            return ContentUnavailable(exception, loggerFactory, "validating manual requirements");
        }

        var sourceName = string.IsNullOrWhiteSpace(request.SourceName) ? "Manual entry" : request.SourceName.Trim();
        var sourceText = request.SourceText ?? "";

        var draft = RequirementDraft.Normalize(ToDraft(request));
        var errors = RequirementValidation.Validate(draft, catalogue);

        if (errors.Count > 0)
        {
            return Failure(
                StatusCodes.Status400BadRequest,
                "These requirements need a fix before they can be saved.",
                sourceName,
                sourceText,
                errors);
        }

        var set = NewSet(draft, sourceName, sourceText, "manual", "", DateTimeOffset.UtcNow);

        db.RequirementSets.Add(set);
        await db.SaveChangesAsync(ct);

        return TypedResults.Created($"/api/requirements/{set.Id}", ToDto(set, catalogue));
    }

    private static Task<RequirementSet?> LoadAsync(RequirementsDbContext db, string id, CancellationToken ct) =>
        db.RequirementSets
            .Include(set => set.Skills)
            .FirstOrDefaultAsync(set => set.Id == id, ct);

    private static RequirementSet NewSet(
        ParsedRequirements draft,
        string sourceName,
        string sourceText,
        string provider,
        string model,
        DateTimeOffset parsedAt)
    {
        var id = $"req-{Guid.NewGuid():N}";

        return new RequirementSet
        {
            Id = id,
            SourceName = sourceName,
            SourceText = sourceText,
            Purpose = draft.Purpose,
            Criticality = draft.Criticality,
            Provider = provider,
            Model = model,
            ParsedAt = parsedAt,
            Edited = false,
            // The first normalized parse, so the draft and the leader's edits are both recoverable.
            OriginalJson = RequirementsJson.Write(draft),
            OutsideRoleJson = RequirementsJson.Write(draft.OutsideRole),
            Skills = draft.Skills.Select(skill => new RequirementSkill
            {
                Id = $"rs-{Guid.NewGuid():N}",
                SetId = id,
                SkillId = skill.SkillId,
                MinLevel = skill.MinLevel,
                Headcount = skill.Headcount,
                Rationale = skill.Rationale
            }).ToList()
        };
    }

    private static RequirementSetDto ToDto(RequirementSet set, IReadOnlyList<SkillDto> catalogue)
    {
        var names = catalogue
            .DistinctBy(skill => skill.Id)
            .ToDictionary(skill => skill.Id, skill => skill.Name, StringComparer.Ordinal);

        var skills = set.Skills
            .Select(skill => new RequirementSkillDto(
                skill.SkillId,
                names.TryGetValue(skill.SkillId, out var name) ? name : skill.SkillId,
                skill.MinLevel,
                skill.Headcount,
                skill.Rationale))
            .OrderBy(skill => skill.Name, StringComparer.Ordinal)
            .ThenBy(skill => skill.SkillId, StringComparer.Ordinal)
            .ToArray();

        return new RequirementSetDto(
            set.Id,
            set.SourceName,
            set.SourceText,
            set.Purpose,
            set.Criticality,
            set.Provider,
            set.Model,
            set.ParsedAt,
            set.ConfirmedAt,
            set.Edited,
            skills,
            RequirementsJson.Read<IReadOnlyList<string>>(set.OutsideRoleJson, []));
    }

    private static ParsedRequirements ToDraft(UpdateRequirementSetRequest request) => new()
    {
        Purpose = request.Purpose,
        Criticality = request.Criticality,
        Skills = (request.Skills ?? [])
            .Select(skill => new ParsedRequirementSkill(skill.SkillId, skill.MinLevel, skill.Headcount, skill.Rationale))
            .ToList(),
        OutsideRole = (request.OutsideRole ?? []).ToList()
    };

    private static ParsedRequirements ToDraft(ManualRequirementSetRequest request) => new()
    {
        Purpose = request.Purpose,
        Criticality = request.Criticality,
        Skills = (request.Skills ?? [])
            .Select(skill => new ParsedRequirementSkill(skill.SkillId, skill.MinLevel, skill.Headcount, skill.Rationale))
            .ToList(),
        OutsideRole = (request.OutsideRole ?? []).ToList()
    };

    private static JsonHttpResult<ParseFailureDto> Failure(
        int statusCode,
        string detail,
        string sourceName,
        string sourceText,
        IReadOnlyList<string> errors) =>
        TypedResults.Json(new ParseFailureDto(detail, sourceName, sourceText, errors), statusCode: statusCode);

    private static ProblemHttpResult ContentUnavailable(
        Exception exception,
        ILoggerFactory loggerFactory,
        string operation)
    {
        loggerFactory.CreateLogger("Requirements.Content")
            .LogWarning(exception, "Content is unavailable while {Operation}", operation);

        return TypedResults.Problem(
            title: "Content is unavailable",
            detail: "The skill catalogue could not be read, so requirements cannot be checked right now.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?> { ["service"] = "content" });
    }
}
