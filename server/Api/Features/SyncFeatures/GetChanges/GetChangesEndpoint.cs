using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class GetChangesEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/workspaces/{workspaceId:guid}/sync/changes", async (
            [FromRoute] Guid workspaceId,
            [FromQuery] long since,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var query = new GetChangesQuery(workspaceId, since);
            var result = await dispatcher.QueryAsync<GetChangesQuery, SyncDeltaBatch>(query, cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("Sync");
    }
}
