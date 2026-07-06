using Microsoft.AspNetCore.Mvc;

namespace Api;

public record LoginRequest(string Email, string Password);

public static class LoginEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", async (
            [FromBody] LoginRequest request,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(request.Email, request.Password);
            var result = await dispatcher.SendAsync<LoginCommand, LoginResponse>(command, cancellationToken);
            return result.ToMinimalResult();
        })
        .WithTags("Auth");
    }
}
