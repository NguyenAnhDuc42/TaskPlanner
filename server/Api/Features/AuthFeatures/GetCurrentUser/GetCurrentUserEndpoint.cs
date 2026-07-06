using Microsoft.AspNetCore.Mvc;

namespace Api;

public static class GetCurrentUserEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/me", async (
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.QueryAsync<GetCurrentUserQuery, GetCurrentUserDto>(new GetCurrentUserQuery(), cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("Auth");
    }
}
