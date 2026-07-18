using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class UpdateDocumentEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/documents/sync/{id:guid}", async (
            Guid id,
            [FromBody] UpdateDocumentCommand request,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required for offline sync." });
            }

            request.DocumentId = id;
            request.TraceId = traceId;

            var result = await dispatcher.SendAsync<UpdateDocumentCommand, long>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { SyncEventId = result.Value })
                : MinimalResultExtensions.Problem(result.Error!);
        })
        .RequireAuthorization()
        .WithTags("DocumentsSync");
    }
}
