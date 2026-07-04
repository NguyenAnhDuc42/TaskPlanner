using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class FetchNotificationsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        // /sync suffix to coexist with the legacy GET /api/Notifications route.
        app.MapGet("/api/notifications/sync", async (
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken,
            [FromQuery] string? cursor,
            [FromQuery] int limit,
            [FromQuery] bool unreadOnly = false) =>
        {
            var query = new FetchNotificationsQuery(cursor, limit == 0 ? 20 : limit, unreadOnly);
            var result = await dispatcher.QueryAsync<FetchNotificationsQuery, FetchNotificationsResult>(query, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MinimalResultExtensions.Problem(result.Error!);
        })
        .RequireAuthorization()
        .WithTags("NotificationsSync");
    }
}
