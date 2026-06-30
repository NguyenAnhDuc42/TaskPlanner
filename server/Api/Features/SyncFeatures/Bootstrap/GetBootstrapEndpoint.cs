using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class GetBootstrapEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/workspaces/{workspaceId:guid}/sync/bootstrap", async (
            [FromRoute] Guid workspaceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var query = new GetBootstrapQuery(workspaceId);
            var result = await dispatcher.QueryAsync<GetBootstrapQuery, BootstrapResult>(query, cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("Sync");
    }
}
