using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class DeleteFolderEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/folders/sync/{id:guid}", async (
            Guid id,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required for offline sync." });
            }

            var request = new DeleteFolderCommand { FolderId = id, TraceId = traceId };

            var result = await dispatcher.SendAsync<DeleteFolderCommand, long>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { SyncEventId = result.Value })
                : MinimalResultExtensions.Problem(result.Error!);
        })
        .RequireAuthorization()
        .WithTags("FoldersSync");
    }
}
