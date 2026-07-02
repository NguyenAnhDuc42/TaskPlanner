using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class UpdateWorkspaceEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/workspaces/sync/{id:guid}", async (
            Guid id,
            [FromBody] UpdateWorkspaceCommand request,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required for offline sync." });
            }

            request.WorkspaceId = id;
            request.TraceId = traceId;

            var result = await dispatcher.SendAsync<UpdateWorkspaceCommand, long>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { SyncEventId = result.Value })
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization()
        .WithTags("WorkspacesSync");
    }
}
