using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Api;

public static class ExternalLoginEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/auth/external-login/{provider}", (
            string provider,
            IOptions<AppSettings> appOptions) =>
        {
            var scheme = provider.ToLower() switch
            {
                "google" => "Google",
                "github" => "GitHub",
                _ => provider
            };
            // Must stay on the backend's own domain — the OAuth handler's internal callback
            // (Google → /signin-google) lands directly on Railway and sets a temporary "External"
            // cookie there to carry the exchanged claims through to OAuthFinish. Redirecting this
            // to the frontend domain makes the browser hop to Vercel before OAuthFinish runs, so
            // that External cookie never arrives — AuthenticateAsync("External") fails instantly.
            var finishUrl = $"{appOptions.Value.BackendUrl}/api/auth/oauth-finish/{provider}";
            var props = new AuthenticationProperties { RedirectUri = finishUrl };
            return Results.Challenge(props, [scheme]);
        })
        .WithTags("Auth");
    }
}
