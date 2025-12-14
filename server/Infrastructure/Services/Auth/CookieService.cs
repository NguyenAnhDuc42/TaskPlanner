using System;
using System.IdentityModel.Tokens.Jwt;
using Application.Features.Auth.DTOs;
using Infrastructure.Auth.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using server.Application.Interfaces;

namespace Infrastructure.Auth;

public class CookieService : ICookieService
{
    private readonly CookieSettings _cookieSettings;
    private readonly ILogger<CookieService> _logger;
    public CookieService(IOptions<CookieSettings> cookieSettings, ILogger<CookieService> logger)
    {
        _cookieSettings = cookieSettings.Value;
        _logger = logger;
    }

    public void ClearAuthCookies(HttpContext context)
    {
        var accessCookieOpt = new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookieSettings.UseSecure,
            SameSite = _cookieSettings.SameSite,
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            Domain = string.IsNullOrEmpty(_cookieSettings.Domain) ? null : _cookieSettings.Domain
        };

        var refreshCookieOpt = new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookieSettings.UseSecure,
            SameSite = _cookieSettings.SameSite,
            Expires = DateTimeOffset.UtcNow.AddDays(-30),
            Domain = string.IsNullOrEmpty(_cookieSettings.Domain) ? null : _cookieSettings.Domain
        };
        try
        {
            context.Response.Cookies.Append(_cookieSettings.AccessTokenCookieName, "", accessCookieOpt);
            context.Response.Cookies.Append(_cookieSettings.RefreshTokenCookieName, "", refreshCookieOpt);
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Failed to clear auth cookies.");
        }
    }

    public JwtTokens? GetAuthTokensFromCookies(HttpContext context)
    {
        var accessToken = context.Request.Cookies[_cookieSettings.AccessTokenCookieName];
        var refreshToken = context.Request.Cookies[_cookieSettings.RefreshTokenCookieName];
        if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
        {
            return null;
        }
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(accessToken);
            var expirationAccessToken = jwtToken.ValidTo;
            return new JwtTokens(accessToken, refreshToken, expirationAccessToken, default);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    public string GetRefreshTokenFromCookies(HttpContext context)
    {
        string? refreshToken = null;
        try
        {
            refreshToken = context.Request.Cookies[_cookieSettings.RefreshTokenCookieName];
        }
        catch (Exception)
        {
            _logger.LogError("Failed to read refresh token from cookies.");
        }
        if (string.IsNullOrEmpty(refreshToken))
        {
            throw new ArgumentException("Refresh token is not found in cookies.");
        }
        return refreshToken;
    }

    public void SetAuthCookies(HttpContext context, JwtTokens tokens)
    {
        var accessCookieOpt = new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookieSettings.UseSecure,
            SameSite = _cookieSettings.SameSite,
            Expires = tokens.ExpirationAccessToken,
            Domain = string.IsNullOrEmpty(_cookieSettings.Domain) ? null : _cookieSettings.Domain
        };

        var refreshCookieOpt = new CookieOptions
        {
            HttpOnly = true,
            Secure = _cookieSettings.UseSecure,
            SameSite = _cookieSettings.SameSite,
            Expires = tokens.ExpirationRefreshToken,
            Domain = string.IsNullOrEmpty(_cookieSettings.Domain) ? null : _cookieSettings.Domain
        };

        context.Response.Cookies.Append(_cookieSettings.AccessTokenCookieName, tokens.AccessToken, accessCookieOpt);
        context.Response.Cookies.Append(_cookieSettings.RefreshTokenCookieName, tokens.RefreshToken, refreshCookieOpt);

    }
}
