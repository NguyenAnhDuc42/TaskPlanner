using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;


namespace Api;

[Route("api/auth")]
[ApiController]
public class AuthController(IHandler handler, IOptions<AppSettings> appOptions) : ControllerBase
{
    private readonly IHandler _handler = handler;
    private readonly string _frontendUrl = appOptions.Value.FrontendUrl;
    private readonly string _backendUrl = appOptions.Value.BackendUrl;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _handler.SendAsync<LoginCommand, LoginResponse>(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync(new LogoutCommand(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.UserName, request.Email, request.Password);
        var result = await _handler.SendAsync<RegisterCommand, RegisterResponse>(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync<RefreshTokenCommand, RefreshTokenResponse>(new RefreshTokenCommand(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _handler.SendAsync<ForgotPasswordCommand, string?>(new ForgotPasswordCommand(request.Email), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword);
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("external-login/{provider}")]
    public IActionResult ExternalLogin(string provider)
    {
        var scheme = provider.ToLower() switch
        {
            "google" => "Google",
            "github" => "GitHub",
            _ => provider
        };
        // Must stay on the backend's own domain — the OAuth handler's internal callback (Google
        // → /signin-google) lands directly on Railway and sets a temporary "External" cookie
        // there to carry the exchanged claims through to OAuthFinish. Redirecting this to the
        // frontend domain (tried earlier) makes the browser hop to Vercel before OAuthFinish
        // runs, so that External cookie never arrives — AuthenticateAsync("External") fails
        // instantly. OAuthFinish itself still needs a proper fix for its OWN redirect to the
        // frontend afterward (session cookies it sets are scoped to Railway, useless to Vercel) —
        // tracked separately, not solved by this URL alone.
        var finishUrl = $"{_backendUrl}/api/auth/oauth-finish/{provider}";
        var props = new AuthenticationProperties { RedirectUri = finishUrl };
        return Challenge(props, scheme);
    }

    // Called by OAuth middleware after successful token exchange — NOT the OAuth callback path
    [HttpGet("oauth-finish/{provider}")]
    public async Task<IActionResult> OAuthFinish(string provider, CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync("External");
        if (!result.Succeeded)
            return Redirect($"{_frontendUrl}/auth/sign-in?error=oauth_failed");

        var claims = result.Principal!.Claims;
        var externalId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(externalId) || string.IsNullOrEmpty(email))
            return Redirect($"{_frontendUrl}/auth/sign-in?error=oauth_missing_info");

        var command = new OAuthCallbackCommand(provider, externalId, email, string.IsNullOrWhiteSpace(name) ? email : name);
        var cmdResult = await _handler.SendAsync<OAuthCallbackCommand, LoginResponse>(command, cancellationToken);

        if (!cmdResult.IsSuccess)
            return Redirect($"{_frontendUrl}/auth/sign-in?error=oauth_failed");

        await HttpContext.SignOutAsync("External");
        return Redirect($"{_frontendUrl}/?select=true");
    }

    [HttpPost("change-password")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(request.CurrentPassword, request.NewPassword);
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await _handler.QueryAsync<GetCurrentUserQuery, GetCurrentUserDto>(new GetCurrentUserQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateProfileCommand(request.Name, request.Email);
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }

    // SignalR hub connections go directly to the backend's own origin (Vercel can't proxy a
    // WebSocket upgrade), which is cross-domain from the frontend — the HttpOnly auth cookie is
    // scoped to the frontend's domain (set via the Vercel proxy round-trip) and can never reach
    // the backend domain directly. This endpoint is called the normal way (same-origin via the
    // Vercel proxy, cookie works fine here), handing back the already-valid access token so the
    // frontend can pass it as ?access_token=... on the hub URL instead. See DependencyInjection.cs
    // OnMessageReceived, which only accepts this for /hubs paths.
    [HttpGet("signalr-token")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public IActionResult GetSignalRToken([FromServices] CookieService cookieService)
    {
        var tokens = cookieService.GetAuthTokensFromCookies();
        if (tokens is null) return Unauthorized();
        return Ok(new { accessToken = tokens.AccessToken });
    }
}

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string UserName, string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ExternalLoginRequest(string Provider, string Token);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record UpdateProfileRequest(string? Name, string? Email);


