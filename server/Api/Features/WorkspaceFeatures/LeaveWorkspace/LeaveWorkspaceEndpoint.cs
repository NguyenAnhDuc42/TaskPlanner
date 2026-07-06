namespace Api;

public static class LeaveWorkspaceEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workspaces/{id:guid}/leave", async (
            Guid id,
            IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync(new LeaveWorkspaceCommand(id), cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("WorkspacesSync");
    }
}
