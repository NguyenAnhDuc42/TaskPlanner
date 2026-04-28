using System.IdentityModel.Tokens.Jwt;
using Application.Features.Auth;
using Infrastructure.Auth.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Application.Interfaces;

namespace Infrastructure.Auth;

public class CookieService(
    IOptions<CookieSettings> cookieSettings, 
    IHttpContextAccessor httpContextAccessor
) : ICookieService
{
    private readonly CookieSettings _settings = cookieSettings.Value;

    private HttpContext? Context => httpContextAccessor.HttpContext;

    public void SetAuthCookies(JwtTokens tokens)
    {
        if (Context == null) return;

        // 1. Access Token (HttpOnly)
        var accessOpt = CreateBaseOptions();
        accessOpt.HttpOnly = true;
        accessOpt.Expires = tokens.ExpirationAccessToken;
        Context.Response.Cookies.Append(_settings.AccessTokenCookieName, tokens.AccessToken, accessOpt);

        // 2. Refresh Token (HttpOnly)
        var refreshOpt = CreateBaseOptions();
        refreshOpt.HttpOnly = true;
        refreshOpt.Expires = tokens.ExpirationRefreshToken;
        Context.Response.Cookies.Append(_settings.RefreshTokenCookieName, tokens.RefreshToken, refreshOpt);

        // 3. Expiry Signal (JS Accessible)
        var expiryOpt = CreateBaseOptions();
        expiryOpt.HttpOnly = false;
        expiryOpt.Expires = tokens.ExpirationAccessToken;
        Context.Response.Cookies.Append(_settings.AccessTokenExpireCookieName, tokens.ExpirationAccessToken.ToUnixTimeSeconds().ToString(), expiryOpt);

        // 4. Session Gatekeeper (JS Accessible)
        var sessionOpt = CreateBaseOptions();
        sessionOpt.HttpOnly = false;
        sessionOpt.Expires = tokens.ExpirationRefreshToken;
        Context.Response.Cookies.Append(_settings.SessionPresentCookieName, "true", sessionOpt);
    }

    public void ClearAuthCookies()
    {
        if (Context == null) return;

        var expiry = DateTimeOffset.UtcNow.AddDays(-1);

        var httpOnlyOpt = CreateBaseOptions();
        httpOnlyOpt.HttpOnly = true;
        httpOnlyOpt.Expires = expiry;

        var jsOpt = CreateBaseOptions();
        jsOpt.HttpOnly = false;
        jsOpt.Expires = expiry;

        Context.Response.Cookies.Append(_settings.AccessTokenCookieName, "", httpOnlyOpt);
        Context.Response.Cookies.Append(_settings.RefreshTokenCookieName, "", httpOnlyOpt);
        Context.Response.Cookies.Append(_settings.AccessTokenExpireCookieName, "", jsOpt);
        Context.Response.Cookies.Append(_settings.SessionPresentCookieName, "", jsOpt);
    }

    public string? GetRefreshTokenFromCookies()
    {
        return Context?.Request.Cookies[_settings.RefreshTokenCookieName];
    }

    public JwtTokens? GetAuthTokensFromCookies()
    {
        if (Context == null) return null;

        var access = Context.Request.Cookies[_settings.AccessTokenCookieName];
        var refresh = Context.Request.Cookies[_settings.RefreshTokenCookieName];
        
        if (string.IsNullOrEmpty(access) || string.IsNullOrEmpty(refresh)) return null;

        try
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(access);
            return new JwtTokens(access, refresh, jwt.ValidTo, default);
        }
        catch { return null; }
    }

    private CookieOptions CreateBaseOptions() => new()
    {
        Path = "/",
        Secure = _settings.UseSecure,
        SameSite = _settings.SameSite,
        Domain = string.IsNullOrEmpty(_settings.Domain) ? null : _settings.Domain
    };
}
