using System;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using src.Infrastructure.Abstractions.IServices;

namespace src.Infrastructure.Services;

public record CookieSettings
{
    public bool UseSecure { get; init; } = true;
    public string AccessTokenCookieName { get; init; } = "acct";
    public string RefreshTokenCookieName { get; init; } = "rft";
    public string Domain { get; init; } = string.Empty;
    public SameSiteMode SameSite { get; init; } = SameSiteMode.Strict;
}

public class CookieService : ICookieService
{
    private readonly CookieSettings _settings;
    private readonly JwtSettings _jwtSettings;
    public CookieService(IOptions<CookieSettings> settings, IOptions<JwtSettings> jwtSettings)
    {
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
    }
    public void ClearAuthCookies(HttpContext context)
    {
        var accessCookieOpt = new CookieOptions
        {
            HttpOnly = true,
            Secure = _settings.UseSecure,
            SameSite = _settings.SameSite,
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            Domain = string.IsNullOrEmpty(_settings.Domain) ? null : _settings.Domain
        };

        var refreshCookieOpt = new CookieOptions
        {
            HttpOnly = true,
            Secure = _settings.UseSecure,
            SameSite = _settings.SameSite,
            Expires = DateTimeOffset.UtcNow.AddDays(-30),
            Domain = string.IsNullOrEmpty(_settings.Domain) ? null : _settings.Domain
        };

        context.Response.Cookies.Append(_settings.AccessTokenCookieName,"", accessCookieOpt);
        context.Response.Cookies.Append(_settings.RefreshTokenCookieName, "", refreshCookieOpt);
    }

    public JwtTokens? GetAuthTokensFromCookies(HttpContext context)
    {
        var accessToken = context.Request.Cookies[_settings.AccessTokenCookieName];
        var refreshToken = context.Request.Cookies[_settings.RefreshTokenCookieName];
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            return null;
        }
        try
        {
            var tokenHanlder = new JwtSecurityTokenHandler();
            var jwtToken = tokenHanlder.ReadJwtToken(accessToken);
            var expirationAccessToken = jwtToken.ValidTo;
            return new JwtTokens(
                accessToken,
                refreshToken,
                expirationAccessToken,
                default
            );
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public string GetRefreshTokenFromCookies(HttpContext context)
    {
        var refreshTokne = context.Request.Cookies[_settings.RefreshTokenCookieName];
        if (string.IsNullOrEmpty(refreshTokne))
        {
            throw new ArgumentException("Refresh token is not found in cookies.");
        }
        return refreshTokne;
    }

    public void SetAuthCookies(HttpContext context, JwtTokens tokens)
    {
        var accessCookieOpt = new CookieOptions
        {
            HttpOnly = true,
            Secure = _settings.UseSecure,
            SameSite = _settings.SameSite,
            Expires = tokens.ExpirationAccessToken,
            Domain = string.IsNullOrEmpty(_settings.Domain) ? null : _settings.Domain
        };

        var refreshCookieOpt = new CookieOptions
        {
            HttpOnly = true,
            Secure = _settings.UseSecure,
            SameSite = _settings.SameSite,
            Expires = tokens.ExpirationRefreshToken,
            Domain = string.IsNullOrEmpty(_settings.Domain) ? null : _settings.Domain
        };

        context.Response.Cookies.Append(_settings.AccessTokenCookieName, tokens.AccessToken, accessCookieOpt);
        context.Response.Cookies.Append(_settings.RefreshTokenCookieName, tokens.RefreshToken, refreshCookieOpt);
    }
}
