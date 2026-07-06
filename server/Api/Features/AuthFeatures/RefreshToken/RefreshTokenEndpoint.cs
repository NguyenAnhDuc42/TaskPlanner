using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class RefreshTokenEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/refresh", async (
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync<RefreshTokenCommand, RefreshTokenResponse>(new RefreshTokenCommand(), cancellationToken);
            return result.ToMinimalResult();
        })
        .WithTags("Auth");
    }
}
