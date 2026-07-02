using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class UpdateDocumentBlockEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/document-blocks/sync/{id:guid}", async (
            Guid id,
            [FromBody] UpdateDocumentBlockCommand request,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required for offline sync." });
            }

            request.BlockId = id;
            request.TraceId = traceId;

            var result = await dispatcher.SendAsync<UpdateDocumentBlockCommand, long>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { SyncEventId = result.Value })
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization()
        .WithTags("DocumentBlocksSync");
    }
}
