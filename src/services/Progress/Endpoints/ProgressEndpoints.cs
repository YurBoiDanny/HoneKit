using HoneKit.Contracts;
using HoneKit.Progress.Clients;
using HoneKit.Progress.Data;
using HoneKit.Progress.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HoneKit.Progress.Endpoints;

public static class ProgressEndpoints
{
    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        // Unknown members simply have no completions; Progress does not own the member list.
        api.MapGet("/members/{id}/completions", async Task<Ok<CompletionsDto>> (
            string id, ProgressDbContext db, CancellationToken ct) =>
        {
            var lessonIds = await db.LessonCompletions.AsNoTracking()
                .Where(c => c.MemberId == id)
                .OrderBy(c => c.LessonId)
                .Select(c => c.LessonId)
                .ToArrayAsync(ct);

            return TypedResults.Ok(new CompletionsDto(id, lessonIds));
        });

        // Start the deck: only the card types this slice can grade join the queue, in deck order.
        api.MapPost("/sessions", async Task<Results<Created<SessionDto>, NotFound, ProblemHttpResult>> (
            StartSessionRequest request,
            ProgressDbContext db,
            ContentClient content,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            DeckPayload? deck;
            try
            {
                deck = await content.GetDeckAsync(request.DeckId, ct);
            }
            catch (Exception exception)
            {
                return ContentUnavailable(exception, loggerFactory);
            }

            var cardIds = deck?.Cards
                .Where(card => string.Equals(card.Type, ContentClient.FillInTheBlank, StringComparison.Ordinal))
                .Select(card => card.Id)
                .ToArray() ?? [];

            if (deck is null || cardIds.Length == 0)
            {
                return TypedResults.NotFound();
            }

            var session = new Session
            {
                Id = $"sess-{Guid.NewGuid():N}",
                MemberId = request.MemberId,
                DeckId = deck.Id,
                StartedAt = DateTimeOffset.UtcNow,
                TotalCards = cardIds.Length,
                QueueJson = ProgressJson.Write(cardIds)
            };

            db.Sessions.Add(session);
            await db.SaveChangesAsync(ct);

            return TypedResults.Created(
                $"/api/sessions/{session.Id}",
                new SessionDto(session.Id, session.MemberId, session.DeckId, session.TotalCards, cardIds.Length));
        });

        api.MapGet("/sessions/{id}/next", async Task<Results<Ok<NextCardDto>, NotFound>> (
            string id, ProgressDbContext db, CancellationToken ct) =>
        {
            var session = await db.Sessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
            if (session is null)
            {
                return TypedResults.NotFound();
            }

            var queue = ReadQueue(session);
            return TypedResults.Ok(new NextCardDto(ProgressDomain.Next(queue), queue.Count));
        });

        api.MapPost("/sessions/{id}/attempts", async Task<Results<Ok<AttemptResultDto>, NotFound, BadRequest<string>, ProblemHttpResult>> (
            string id,
            AttemptRequest request,
            ProgressDbContext db,
            ContentClient content,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (session is null)
            {
                return TypedResults.NotFound();
            }

            if (session.FinishedAt is not null)
            {
                return TypedResults.BadRequest("This session is finished.");
            }

            var queue = ReadQueue(session);
            if (queue.Count == 0)
            {
                return TypedResults.BadRequest("The queue is empty; finish the session instead.");
            }

            if (!string.Equals(queue[0], request.CardId, StringComparison.Ordinal))
            {
                return TypedResults.BadRequest("That card is not the next one in the queue.");
            }

            CardAnswerDto? answer;
            try
            {
                answer = await content.GetCardAnswerAsync(request.CardId, ct);
            }
            catch (Exception exception)
            {
                return ContentUnavailable(exception, loggerFactory);
            }

            if (answer is null)
            {
                return TypedResults.NotFound();
            }

            // Slice 2 grades fill-in-the-blank by tapping a choice, so the answer is the choice id.
            var submitted = request.Answer.Trim();
            var correct = !string.IsNullOrEmpty(answer.CorrectChoiceId)
                && string.Equals(submitted, answer.CorrectChoiceId, StringComparison.OrdinalIgnoreCase);

            var firstTry = !await db.Attempts.AnyAsync(
                a => a.SessionId == session.Id && a.CardId == request.CardId, ct);

            var now = DateTimeOffset.UtcNow;
            var remaining = ProgressDomain.Remove(queue, request.CardId);
            int? requeuedAfter = null;

            if (!correct)
            {
                remaining = ProgressDomain.RequeueAfterTwo(remaining, request.CardId);
                requeuedAfter = remaining.ToList().IndexOf(request.CardId);
            }

            var dueAt = ProgressDomain.DueAt(correct, now);

            db.Attempts.Add(new Attempt
            {
                Id = $"att-{Guid.NewGuid():N}",
                MemberId = session.MemberId,
                CardId = request.CardId,
                SessionId = session.Id,
                AnswerJson = ProgressJson.Write(new AttemptAnswer(request.Answer)),
                Correct = correct,
                FirstTry = firstTry,
                At = now
            });

            var review = await db.Reviews.FirstOrDefaultAsync(
                r => r.MemberId == session.MemberId && r.CardId == request.CardId, ct);

            if (review is null)
            {
                review = new Review { MemberId = session.MemberId, CardId = request.CardId };
                db.Reviews.Add(review);
            }

            review.IntervalDays = correct ? (int)ProgressDomain.CorrectInterval.TotalDays : 0;
            review.DueAt = dueAt;
            review.LastResult = correct ? "correct" : "wrong";

            session.QueueJson = ProgressJson.Write(remaining);
            await db.SaveChangesAsync(ct);

            string? wrongReason = null;
            if (!correct)
            {
                answer.WrongReasons.TryGetValue(submitted, out var reason);
                wrongReason = reason;
            }

            // The correct choice is returned on a miss too: the member's reveal setting lives in the
            // frontend, and it cannot show the answer without it. It is not a competitive score.
            return TypedResults.Ok(new AttemptResultDto(
                correct,
                answer.CorrectChoiceId,
                answer.Explanation,
                wrongReason,
                requeuedAfter,
                dueAt,
                remaining.Count));
        });

        api.MapPost("/sessions/{id}/finish", async Task<Results<Ok<SessionResultDto>, NotFound, ProblemHttpResult>> (
            string id,
            FinishSessionRequest request,
            ProgressDbContext db,
            ContentClient content,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (session is null)
            {
                return TypedResults.NotFound();
            }

            DeckPayload? deck;
            SkillDto[] skills;
            try
            {
                deck = await content.GetDeckAsync(session.DeckId, ct);
                skills = deck is null ? [] : await content.GetSkillsAsync(deck.Role, ct);
            }
            catch (Exception exception)
            {
                return ContentUnavailable(exception, loggerFactory);
            }

            if (deck is null)
            {
                return TypedResults.NotFound();
            }

            var queue = ReadQueue(session);

            // SQLite cannot ORDER BY a DateTimeOffset, and a session has a handful of attempts, so
            // ordering happens here rather than in SQL.
            var attempts = await db.Attempts.AsNoTracking()
                .Where(a => a.SessionId == session.Id)
                .ToListAsync(ct);

            var answers = attempts
                .OrderBy(a => a.At)
                .Select(a => new GradedAnswer(a.CardId, a.Correct));

            var outcome = ProgressDomain.Evaluate(answers, queue);

            var now = DateTimeOffset.UtcNow;

            session.FinishedAt ??= now;

            // Leaving before the queue is empty is an early exit even if the client says otherwise.
            session.ExitedEarly = request.ExitedEarly || !outcome.Completed;
            session.FirstTryCorrect = outcome.FirstTryCorrect;
            session.TotalCards = outcome.TotalCards;
            session.Passed = outcome.Passed;
            session.Honed = outcome.Honed;

            if (outcome.Completed)
            {
                await MarkLessonsCompleteAsync(db, session.MemberId, deck.Cards, now, ct);
            }

            await db.SaveChangesAsync(ct);

            var skillName = skills.FirstOrDefault(skill => skill.Id == deck.SkillId)?.Name ?? deck.SkillId;

            return TypedResults.Ok(new SessionResultDto(
                session.Id,
                outcome.FirstTryCorrect,
                outcome.TotalCards,
                outcome.ScorePercent,
                outcome.Passed,
                outcome.Honed,
                outcome.ToolGranted,
                deck.SkillId,
                skillName));
        });

        return app;
    }

    private static IReadOnlyList<string> ReadQueue(Session session) =>
        ProgressJson.Read<IReadOnlyList<string>>(session.QueueJson, []);

    // Idempotent: the unique key holds, and a second finish (or a replayed request) adds nothing.
    private static async Task MarkLessonsCompleteAsync(
        ProgressDbContext db,
        string memberId,
        IEnumerable<CardDto> cards,
        DateTimeOffset completedAt,
        CancellationToken ct)
    {
        var lessonIds = cards
            .Select(card => card.LessonId)
            .Where(lessonId => !string.IsNullOrWhiteSpace(lessonId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (lessonIds.Length == 0)
        {
            return;
        }

        var alreadyComplete = (await db.LessonCompletions
                .Where(completion => completion.MemberId == memberId && lessonIds.Contains(completion.LessonId))
                .Select(completion => completion.LessonId)
                .ToListAsync(ct))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var lessonId in lessonIds.Where(lessonId => !alreadyComplete.Contains(lessonId)))
        {
            db.LessonCompletions.Add(new LessonCompletion
            {
                MemberId = memberId,
                LessonId = lessonId,
                CompletedAt = completedAt
            });
        }
    }

    private static ProblemHttpResult ContentUnavailable(Exception exception, ILoggerFactory loggerFactory)
    {
        loggerFactory.CreateLogger("Progress.Content")
            .LogWarning(exception, "Could not reach Content while running a session");

        return TypedResults.Problem(
            title: "Content is unavailable",
            detail: "The session could not be graded because the Content service did not respond.",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            extensions: new Dictionary<string, object?> { ["service"] = "content" });
    }
}
