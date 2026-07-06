using Microsoft.AspNetCore.Mvc;

namespace Api;

public record ForgotPasswordRequest(string Email);

public static class ForgotPasswordEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/forgot-password", async (
            [FromBody] ForgotPasswordRequest request,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var result = await dispatcher.SendAsync<ForgotPasswordCommand, string?>(new ForgotPasswordCommand(request.Email), cancellationToken);
            return result.ToMinimalResult();
        })
        .WithTags("Auth");
    }
}
