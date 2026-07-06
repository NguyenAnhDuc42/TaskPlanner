using Microsoft.AspNetCore.Mvc;

namespace Api;

public record SetWorkspacePinRequest(bool IsPinned);

public static class SetWorkspacePinEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Route kept identical to the legacy WorkspacesController action — frontend's pin()
        // (workspace.mutations.ts) already calls this exact path, no frontend change needed.
        app.MapPut("/api/workspaces/{id:guid}/pin", async (
            Guid id,
            [FromBody] SetWorkspacePinRequest request,
            IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new SetWorkspacePinCommand(id, request.IsPinned);
            var result = await dispatcher.SendAsync<SetWorkspacePinCommand, bool>(command, cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("WorkspacesSync");
    }
}
