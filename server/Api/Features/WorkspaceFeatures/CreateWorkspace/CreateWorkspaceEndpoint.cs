using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class CreateWorkspaceEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/workspaces/sync", async (
            [FromBody] CreateWorkspaceCommand request,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required for offline sync." });
            }

            request.TraceId = traceId;

            var result = await dispatcher.SendAsync<CreateWorkspaceCommand, Guid>(request, cancellationToken);

            if (!result.IsSuccess)
                return MinimalResultExtensions.Problem(result.Error!);

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
