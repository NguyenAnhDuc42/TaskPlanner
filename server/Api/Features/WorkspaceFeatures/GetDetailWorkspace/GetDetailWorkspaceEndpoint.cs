namespace Api;

public static class GetDetailWorkspaceEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Route/response shape kept identical to the legacy WorkspacesController action this
        // replaces — the frontend's fetchDetail() (workspace.mutations.ts) already calls this
        // exact path and expects this exact WorkspaceRecord shape, so no frontend change needed.
        app.MapGet("/api/workspaces/{id:guid}/me/permissions", async (
            Guid id,
            IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.QueryAsync<GetDetailWorkspaceQuery, WorkspaceRecord>(new GetDetailWorkspaceQuery(id), cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("WorkspacesSync");
    }
}
