using HoneKit.Content.Data;
using HoneKit.Content.Domain;
using HoneKit.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HoneKit.Content.Endpoints;

public sealed record DeckDetailDto(
    string Id,
    string Role,
    string Name,
    string SkillId,
    IReadOnlyList<CardDto> Cards);

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/roles/{role}/skills", async Task<Ok<SkillDto[]>> (string role, ContentDbContext db, CancellationToken ct) =>
        {
            var skills = await db.Skills.AsNoTracking()
                .Where(s => s.Role == role)
                .OrderBy(s => s.Name)
                .Select(s => new SkillDto(s.Id, s.Role, s.Name, s.LessonCount))
                .ToArrayAsync(ct);

            return TypedResults.Ok(skills);
        });

        api.MapGet("/roles/{role}/lessons", async Task<Ok<LessonDto[]>> (string role, ContentDbContext db, CancellationToken ct) =>
        {
            var lessons = await (
                    from lesson in db.Lessons.AsNoTracking()
                    join skill in db.Skills.AsNoTracking() on lesson.SkillId equals skill.Id
                    where skill.Role == role
                    orderby skill.Name, lesson.Ordinal
                    select new LessonDto(lesson.Id, lesson.SkillId, lesson.Ordinal, lesson.Title))
                .ToArrayAsync(ct);

            return TypedResults.Ok(lessons);
        });

        api.MapGet("/decks", async Task<Ok<DeckSummaryDto[]>> (string? role, ContentDbContext db, CancellationToken ct) =>
        {
            var query = db.Decks.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(d => d.Role == role);
            }

            var decks = await query
                .OrderBy(d => d.Name)
                .Select(d => new DeckSummaryDto(
                    d.Id,
                    d.Role,
                    d.Name,
                    d.SkillId,
                    db.Cards.Count(c => c.DeckId == d.Id)))
                .ToArrayAsync(ct);

            return TypedResults.Ok(decks);
        });

        api.MapGet("/decks/{id}", async Task<Results<Ok<DeckDetailDto>, NotFound>> (
            string id, ContentDbContext db, CancellationToken ct) =>
        {
            var deck = await db.Decks.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
            if (deck is null)
            {
                return TypedResults.NotFound();
            }

            var cards = await db.Cards.AsNoTracking()
                .Where(c => c.DeckId == id)
                .OrderBy(c => c.Ordinal)
                .ThenBy(c => c.Id)
                .ToArrayAsync(ct);

            return TypedResults.Ok(new DeckDetailDto(
                deck.Id,
                deck.Role,
                deck.Name,
                deck.SkillId,
                cards.Select(ToDto).ToArray()));
        });

        // Authoring: a leader creates a deck for a role, then appends cards to it. Validation is in
        // Domain/AuthoringRules so the editor's rules are testable without a database.
        api.MapPost("/decks", async Task<Results<Created<DeckSummaryDto>, BadRequest<string>>> (
            CreateDeckRequest request, ContentDbContext db, CancellationToken ct) =>
        {
            var role = (request.Role ?? "").Trim().ToLowerInvariant();

            var roleSkills = await db.Skills.AsNoTracking()
                .Where(skill => skill.Role == role)
                .Select(skill => new SkillDto(skill.Id, skill.Role, skill.Name, skill.LessonCount))
                .ToListAsync(ct);

            var errors = AuthoringRules.ValidateDeck(role, request.Name, request.SkillId, roleSkills);
            if (errors.Count > 0)
            {
                return TypedResults.BadRequest(string.Join(" ", errors));
            }

            var deck = new Deck
            {
                Id = $"deck-{Guid.NewGuid():N}",
                Role = role,
                Name = request.Name.Trim(),
                SkillId = request.SkillId.Trim()
            };

            db.Decks.Add(deck);
            await db.SaveChangesAsync(ct);

            return TypedResults.Created(
                $"/api/decks/{deck.Id}",
                new DeckSummaryDto(deck.Id, deck.Role, deck.Name, deck.SkillId, 0));
        });

        api.MapPost("/decks/{id}/cards", async Task<Results<Created<CardDto>, NotFound, BadRequest<string>>> (
            string id, CreateCardRequest request, ContentDbContext db, CancellationToken ct) =>
        {
            var deck = await db.Decks.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == id, ct);
            if (deck is null)
            {
                return TypedResults.NotFound();
            }

            // Look the lesson up by id so "unknown" and "belongs to another skill" stay distinct.
            LessonDto[] lessons = [];

            if (!string.IsNullOrWhiteSpace(request.LessonId))
            {
                var lesson = await db.Lessons.AsNoTracking()
                    .FirstOrDefaultAsync(candidate => candidate.Id == request.LessonId, ct);

                if (lesson is not null)
                {
                    lessons = [new LessonDto(lesson.Id, lesson.SkillId, lesson.Ordinal, lesson.Title)];
                }
            }

            var errors = AuthoringRules.ValidateCard(request, deck.SkillId, lessons);
            if (errors.Count > 0)
            {
                return TypedResults.BadRequest(string.Join(" ", errors));
            }

            var nextOrdinal = (await db.Cards
                .Where(card => card.DeckId == id)
                .MaxAsync(card => (int?)card.Ordinal, ct) ?? -1) + 1;

            var card = new Card
            {
                Id = $"card-{Guid.NewGuid():N}",
                DeckId = id,
                LessonId = request.LessonId,
                Type = request.Type,
                ImageRef = request.ImageRef,
                Prompt = request.Prompt,
                OptionsJson = ContentJson.Write(request.Choices ?? []),
                TargetRectJson = ContentJson.Write(request.TargetRect),
                Explanation = request.Explanation,
                WrongReasonsJson = ContentJson.Write(request.WrongReasons ?? new Dictionary<string, string>()),
                CorrectChoiceId = request.CorrectChoiceId,
                Ordinal = nextOrdinal
            };

            db.Cards.Add(card);
            await db.SaveChangesAsync(ct);

            return TypedResults.Created($"/api/cards/{card.Id}", ToDto(card));
        });

        api.MapGet("/cards/{id}", async Task<Results<Ok<CardDto>, NotFound>> (
            string id, ContentDbContext db, CancellationToken ct) =>
        {
            var card = await db.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
            return card is null ? TypedResults.NotFound() : TypedResults.Ok(ToDto(card));
        });

        // The grading payload. Only Progress calls this; the correct choice never rides on CardDto.
        api.MapGet("/cards/{id}/answer", async Task<Results<Ok<CardAnswerDto>, NotFound>> (
            string id, ContentDbContext db, CancellationToken ct) =>
        {
            var card = await db.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
            if (card is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(new CardAnswerDto(
                card.Id,
                card.CorrectChoiceId,
                card.Explanation,
                ContentJson.Read<IReadOnlyDictionary<string, string>>(card.WrongReasonsJson, new Dictionary<string, string>()),
                ContentJson.Read<RectDto?>(card.TargetRectJson, null)));
        });

        // Gaps become work: assign a deck to the members who are short. Idempotent per deck and
        // member, so re-clicking the gap action adds nothing.
        api.MapPost("/decks/{id}/assign", async Task<Results<NoContent, NotFound>> (
            string id, AssignDeckRequest request, ContentDbContext db, CancellationToken ct) =>
        {
            var deckExists = await db.Decks.AsNoTracking().AnyAsync(deck => deck.Id == id, ct);
            if (!deckExists)
            {
                return TypedResults.NotFound();
            }

            var memberIds = (request.MemberIds ?? [])
                .Where(memberId => !string.IsNullOrWhiteSpace(memberId))
                .Select(memberId => memberId.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (memberIds.Length == 0)
            {
                return TypedResults.NoContent();
            }

            var alreadyAssigned = (await db.Assignments.AsNoTracking()
                    .Where(assignment => assignment.DeckId == id && memberIds.Contains(assignment.MemberId))
                    .Select(assignment => assignment.MemberId)
                    .ToListAsync(ct))
                .ToHashSet(StringComparer.Ordinal);

            var now = DateTimeOffset.UtcNow;

            foreach (var memberId in memberIds.Where(memberId => !alreadyAssigned.Contains(memberId)))
            {
                db.Assignments.Add(new Assignment
                {
                    Id = $"asg-{Guid.NewGuid():N}",
                    DeckId = id,
                    MemberId = memberId,
                    AssignedAt = now
                });
            }

            await db.SaveChangesAsync(ct);

            return TypedResults.NoContent();
        });

        return app;
    }

    private static CardDto ToDto(Card card) => new(
        card.Id,
        card.DeckId,
        card.LessonId,
        card.Type,
        card.ImageRef,
        card.Prompt,
        ContentJson.Read<IReadOnlyList<ChoiceDto>>(card.OptionsJson, []),
        ContentJson.Read<RectDto?>(card.TargetRectJson, null),
        card.Explanation,
        ContentJson.Read<IReadOnlyDictionary<string, string>>(card.WrongReasonsJson, new Dictionary<string, string>()));
}
