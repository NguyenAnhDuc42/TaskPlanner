using Microsoft.AspNetCore.Mvc;

namespace Api;

public record UpdateProfileRequest(string? Name, string? Email);

public static class UpdateProfileEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/auth/profile", async (
            [FromBody] UpdateProfileRequest request,
            [FromServices] IHandler dispatcher,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateProfileCommand(request.Name, request.Email);
            var result = await dispatcher.SendAsync(command, cancellationToken);
            return result.ToMinimalResult();
        })
        .RequireAuthorization()
        .WithTags("Auth");
    }
}
