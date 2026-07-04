using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class MarkNotificationsReadEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        // /sync suffix to coexist with the legacy PUT /api/Notifications/read route.
        app.MapPut("/api/notifications/sync/read", async (
            [FromBody] MarkNotificationsReadCommand request,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok()
                : MinimalResultExtensions.Problem(result.Error!);
        })
        .RequireAuthorization()
        .WithTags("NotificationsSync");
    }
}
