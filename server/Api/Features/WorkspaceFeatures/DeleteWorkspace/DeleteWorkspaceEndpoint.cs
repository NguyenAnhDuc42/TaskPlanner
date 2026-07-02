namespace Api;

public static class DeleteWorkspaceEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/workspaces/{id:guid}", async (
            Guid id,
            IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var request = new DeleteWorkspaceCommand { WorkspaceId = id };

            var result = await dispatcher.SendAsync<DeleteWorkspaceCommand, long>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { SyncEventId = result.Value })
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization()
        .WithTags("WorkspacesSync");
    }
}
