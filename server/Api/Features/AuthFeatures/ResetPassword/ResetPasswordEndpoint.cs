using Microsoft.AspNetCore.Mvc;

namespace Api;

public record ResetPasswordRequest(string Token, string NewPassword);

public static class ResetPasswordEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new ResetPasswordCommand(request.Token, request.NewPassword);
            var result = await dispatcher.SendAsync(command, cancellationToken);
            return result.ToMinimalResult();
        })
        .WithTags("Auth");
    }
}
