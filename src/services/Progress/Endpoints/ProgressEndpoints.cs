using HoneKit.Contracts;
using HoneKit.Progress.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace HoneKit.Progress.Endpoints;

public static class ProgressEndpoints
{
    public static IEndpointRouteBuilder MapProgressEndpoints(this IEndpointRouteBuilder app)
    {
        // Unknown members simply have no completions; Progress does not own the member list.
        app.MapGet("/api/members/{id}/completions", async Task<Ok<CompletionsDto>> (
            string id, ProgressDbContext db, CancellationToken ct) =>
        {
            var lessonIds = await db.LessonCompletions.AsNoTracking()
                .Where(c => c.MemberId == id)
                .OrderBy(c => c.LessonId)
                .Select(c => c.LessonId)
                .ToArrayAsync(ct);

            return TypedResults.Ok(new CompletionsDto(id, lessonIds));
        });

        return app;
    }
}
