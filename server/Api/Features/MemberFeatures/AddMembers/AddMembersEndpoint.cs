using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class AddMembersEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/members/sync/add", async (
            [FromBody] AddMembersCommand request,
            [FromHeader(Name = "X-Client-Trace-Id")] string? traceId,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(traceId))
            {
                return Results.BadRequest(new { Error = "X-Client-Trace-Id header is required for offline sync." });
            }

            request.TraceId = traceId;

            var result = await dispatcher.SendAsync<AddMembersCommand, AddMembersResult>(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MinimalResultExtensions.Problem(result.Error!);
        })
        .RequireAuthorization()
        .WithTags("MembersSync");
    }
}
