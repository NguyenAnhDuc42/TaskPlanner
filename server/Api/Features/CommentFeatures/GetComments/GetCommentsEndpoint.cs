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
            [FromQuery] string? cursor,
            [FromQuery] int pageSize,
            IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var pagination = new CursorPaginationRequest(cursor, pageSize == 0 ? 15 : pageSize);
            var result = await dispatcher.QueryAsync<GetCommentsQuery, PagedResult<CommentRecord>>(new GetCommentsQuery(id, pagination), cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("CommentsSync");
    }
}
