using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class RemoveMembersEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        // POST, not DELETE — a batch of member IDs needs a body, and DELETE bodies
        // are unreliably supported across HTTP clients/proxies.
        app.MapPost("/api/members/sync/remove", async (
            [FromBody] RemoveMembersCommand request,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required for offline sync." });
            }

            request.TraceId = traceId;

            var result = await dispatcher.SendAsync<RemoveMembersCommand, long>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(new { SyncEventId = result.Value })
                : Results.BadRequest(result.Error);
        })
        .RequireAuthorization()
        .WithTags("MembersSync");
    }
}
