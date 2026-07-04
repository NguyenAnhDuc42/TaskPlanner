using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class UpdateFolderEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/folders/sync/{id:guid}", async (
            Guid id,
            [FromBody] UpdateFolderCommand request,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required for offline sync." });
            }

            request.FolderId = id;
            request.TraceId = traceId;

            var result = await dispatcher.SendAsync<UpdateFolderCommand, long>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { SyncEventId = result.Value })
                : MinimalResultExtensions.Problem(result.Error!);
        })
        .RequireAuthorization()
        .WithTags("FoldersSync");
    }
}
