using Microsoft.AspNetCore.Mvc;

namespace Api;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public static class ChangePasswordEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/change-password", async (
            [FromBody] ChangePasswordRequest request,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);
            var result = await dispatcher.SendAsync(command, cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("Auth");
    }
}
