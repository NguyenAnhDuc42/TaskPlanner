using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class JoinWorkspaceByCodeEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        // /sync suffix to coexist with the legacy POST /api/Workspaces/join route.
        app.MapPost("/api/workspaces/sync/join", async (
            [FromBody] JoinWorkspaceByCodeCommand request,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync<JoinWorkspaceByCodeCommand, JoinWorkspaceByCodeResult>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MinimalResultExtensions.Problem(result.Error!);
        })
        .RequireAuthorization()
        .WithTags("WorkspacesSync");
    }
}
