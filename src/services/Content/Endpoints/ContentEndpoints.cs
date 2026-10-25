using HoneKit.Content.Data;
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
                .OrderBy(c => c.Id)
                .ToArrayAsync(ct);

            return TypedResults.Ok(new DeckDetailDto(
                deck.Id,
                deck.Role,
                deck.Name,
                deck.SkillId,
                cards.Select(ToDto).ToArray()));
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
                ContentJson.Read<IReadOnlyDictionary<string, string>>(card.WrongReasonsJson, new Dictionary<string, string>())));
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
