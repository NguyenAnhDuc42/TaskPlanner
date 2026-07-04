using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class DeleteCommentEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/comments/sync/{id:guid}", async (
            Guid id,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required for offline sync." });
            }

            var request = new DeleteCommentCommand { CommentId = id, TraceId = traceId };

            var result = await dispatcher.SendAsync<DeleteCommentCommand, long>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { SyncEventId = result.Value })
                : MinimalResultExtensions.Problem(result.Error!);
        })
        .RequireAuthorization()
        .WithTags("CommentsSync");
    }
}
