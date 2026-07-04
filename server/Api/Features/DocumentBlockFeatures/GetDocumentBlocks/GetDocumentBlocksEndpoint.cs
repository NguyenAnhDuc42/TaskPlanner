using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class GetDocumentBlocksEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/documents/{documentId:guid}/sync/blocks", async (
            [FromRoute] Guid documentId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDocumentBlocksQuery(documentId);
            var result = await dispatcher.QueryAsync<GetDocumentBlocksQuery, List<DocumentBlockRecord>>(query, cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("DocumentBlocksSync");
    }
}
