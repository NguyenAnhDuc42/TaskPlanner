using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class UpdateWorkspaceEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/workspaces/sync/{id:guid}", async (
            Guid id,
            [FromBody] UpdateWorkspaceCommand request,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            request.WorkspaceId = id;

            var result = await dispatcher.SendAsync<UpdateWorkspaceCommand, Guid>(request, cancellationToken);

            if (!result.IsSuccess)
                return Results.BadRequest(result.Error);

            return Results.Ok(new
            {
                id = result.Value,
                name = request.Name,
                slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null,
                color = request.Color,
                icon = request.Icon,
                description = request.Description
            });
        })
        .RequireAuthorization()
        .WithTags("WorkspacesSync");
    }
}
