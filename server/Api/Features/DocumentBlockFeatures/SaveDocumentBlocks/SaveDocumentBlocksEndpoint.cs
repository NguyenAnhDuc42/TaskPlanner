using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class SaveDocumentBlocksEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/documents/{documentId:guid}/blocks", async (
            Guid documentId,
            [FromBody] List<BlockSaveItem> blocks,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new SaveDocumentBlocksCommand(documentId, blocks)
            {
                TraceId = string.IsNullOrWhiteSpace(traceId) ? Guid.NewGuid().ToString() : traceId
            };
            var result = await dispatcher.SendAsync<SaveDocumentBlocksCommand, long>(command, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { SyncEventId = result.Value })
                : MinimalResultExtensions.Problem(result.Error!);
        })
        .RequireAuthorization()
        .WithTags("Documents");
    }
}
