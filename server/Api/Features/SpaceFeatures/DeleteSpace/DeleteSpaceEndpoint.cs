using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class DeleteSpaceEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/spaces/sync/{id:guid}", async (
            Guid id,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required for offline sync." });
            }

            var request = new DeleteSpaceCommand { SpaceId = id, TraceId = traceId };

            var result = await dispatcher.SendAsync<DeleteSpaceCommand, long>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { SyncEventId = result.Value })
                : MinimalResultExtensions.Problem(result.Error!);
        })
        .RequireAuthorization()
        .WithTags("SpacesSync");
    }
}
