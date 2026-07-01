using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class CreateWorkspaceEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workspaces/sync", async (
            [FromBody] CreateWorkspaceCommand request,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync<CreateWorkspaceCommand, Guid>(request, cancellationToken);

            if (!result.IsSuccess)
                return Results.BadRequest(result.Error);

            return Results.Ok(new
            {
                id = request.Id,
                name = request.Name,
                slug = SlugHelper.GenerateSlug(request.Name),
                color = request.Color,
                icon = request.Icon,
                description = request.Description
            });
        })
        .RequireAuthorization()
        .WithTags("WorkspacesSync");
    }
}
