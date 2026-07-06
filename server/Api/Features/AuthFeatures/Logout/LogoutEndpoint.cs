namespace Api;

public static class LogoutEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/logout", async (
            IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync(new LogoutCommand(), cancellationToken);
            return result.ToMinimalResult();
        })
        .WithTags("Auth");
    }
}
