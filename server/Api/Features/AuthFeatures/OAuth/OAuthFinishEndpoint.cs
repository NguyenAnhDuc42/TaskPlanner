using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Api;

public static class OAuthFinishEndpoint
{
    // Called by OAuth middleware after successful token exchange — NOT the OAuth callback path.
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/oauth-finish/{provider}", async (
            string provider,
            HttpContext httpContext,
            IHandler dispatcher,
            IOptions<AppSettings> appOptions,
            CancellationToken cancellationToken) =>
        {
            var frontendUrl = appOptions.Value.FrontendUrl;

            var result = await httpContext.AuthenticateAsync("External");
            if (!result.Succeeded)
                return Results.Redirect($"{frontendUrl}/auth/sign-in?error=oauth_failed");

            var claims = result.Principal!.Claims;
            var externalId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(email))
                return Results.Redirect($"{frontendUrl}/auth/sign-in?error=oauth_missing_info");

            var command = new OAuthCallbackCommand(provider, externalId, email, string.IsNullOrWhiteSpace(name) ? email : name);
            var cmdResult = await dispatcher.SendAsync<OAuthCallbackCommand, LoginResponse>(command, cancellationToken);

            if (!cmdResult.IsSuccess)
                return Results.Redirect($"{frontendUrl}/auth/sign-in?error=oauth_failed");

            await httpContext.SignOutAsync("External");
            return Results.Redirect($"{frontendUrl}/?select=true");
        })
        .WithTags("Auth");
    }
}
