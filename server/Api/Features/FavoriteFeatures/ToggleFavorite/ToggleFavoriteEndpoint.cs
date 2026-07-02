using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class ToggleFavoriteEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        // No /sync suffix — backend-first, no TransactionQueue entry, same convention as
        // DeleteWorkspaceEndpoint. Still requires a trace ID: unlike Workspace delete, a retried
        // toggle would double-flip the state if not deduped.
        app.MapPost("/api/favorites/toggle", async (
            [FromBody] ToggleFavoriteCommand request,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required." });
            }

            request.TraceId = traceId;

            var result = await dispatcher.SendAsync<ToggleFavoriteCommand, ToggleFavoriteResult>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization()
        .WithTags("FavoritesSync");
    }
}
