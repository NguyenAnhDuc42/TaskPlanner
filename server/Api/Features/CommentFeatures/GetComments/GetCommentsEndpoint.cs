using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class GetCommentsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Route kept identical to the legacy TasksController action — task-comments.tsx already
        // calls this exact path directly (bypassing comment.mutations.ts), no frontend change needed.
        app.MapGet("/api/tasks/{id:guid}/comments", async (
            Guid id,
            IHandler dispatcher,
            CancellationToken cancellationToken,
            [FromQuery] string? cursor = null,
            [FromQuery] int pageSize = 15) =>
        {
            var pagination = new CursorPaginationRequest(cursor, pageSize);
            var result = await dispatcher.QueryAsync<GetCommentsQuery, PagedResult<CommentRecord>>(new GetCommentsQuery(id, pagination), cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("CommentsSync");
    }
}
