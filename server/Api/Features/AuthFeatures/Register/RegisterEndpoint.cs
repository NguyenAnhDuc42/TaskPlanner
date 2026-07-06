using Microsoft.AspNetCore.Mvc;

namespace Api;

public record RegisterRequest(string UserName, string Email, string Password);

public static class RegisterEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (
            [FromBody] RegisterRequest request,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterCommand(request.UserName, request.Email, request.Password);
            var result = await dispatcher.SendAsync<RegisterCommand, RegisterResponse>(command, cancellationToken);
            return result.ToMinimalResult();
        })
        .WithTags("Auth");
    }
}
