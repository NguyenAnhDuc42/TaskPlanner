using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class CreateDocumentBlockEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/document-blocks/sync", async (
            [FromBody] CreateDocumentBlockCommand request,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required for offline sync." });
            }

            request.TraceId = traceId;

            var result = await dispatcher.SendAsync<CreateDocumentBlockCommand, long>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { SyncEventId = result.Value })
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization()
        .WithTags("DocumentBlocksSync");
    }
}
