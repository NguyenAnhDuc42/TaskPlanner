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
        var nonHttpOnlyOpt = new CookieOptions
        {
            HttpOnly = false,
            Secure = _cookieSettings.UseSecure,
            SameSite = _cookieSettings.SameSite,
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            Domain = string.IsNullOrEmpty(_cookieSettings.Domain) ? null : _cookieSettings.Domain
        };

        try
        {
            context.Response.Cookies.Append(_cookieSettings.AccessTokenCookieName, "", accessCookieOpt);
            context.Response.Cookies.Append(_cookieSettings.RefreshTokenCookieName, "", refreshCookieOpt);
            context.Response.Cookies.Append(_cookieSettings.AccessTokenExpireCookieName, "", nonHttpOnlyOpt);
            context.Response.Cookies.Append(_cookieSettings.SessionPresentCookieName, "", nonHttpOnlyOpt);
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

    public string? GetRefreshTokenFromCookies(HttpContext context)
    {
        try
        {
            return context.Request.Cookies[_cookieSettings.RefreshTokenCookieName];
        }
        catch (Exception)
        {
            _logger.LogError("Failed to read refresh token from cookies.");
            return null;
        }
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

        // Non-HttpOnly cookie for JS to see access token expiry
        // This is for the "Proactive Refresh" logic
        var expiryCookieOpt = new CookieOptions
        {
            HttpOnly = false,
            Secure = _cookieSettings.UseSecure,
            SameSite = _cookieSettings.SameSite,
            Expires = tokens.ExpirationAccessToken,
            Domain = string.IsNullOrEmpty(_cookieSettings.Domain) ? null : _cookieSettings.Domain
        };

        // Non-HttpOnly cookie for JS to see if a session EXISTS
        // This is the "Gatekeeper" that lasts as long as the Refresh Token
        var sessionPresentOpt = new CookieOptions
        {
            HttpOnly = false,
            Secure = _cookieSettings.UseSecure,
            SameSite = _cookieSettings.SameSite,
            Expires = tokens.ExpirationRefreshToken, // Full session life
            Domain = string.IsNullOrEmpty(_cookieSettings.Domain) ? null : _cookieSettings.Domain
        };

        context.Response.Cookies.Append(_cookieSettings.AccessTokenCookieName, tokens.AccessToken, accessCookieOpt);
        context.Response.Cookies.Append(_cookieSettings.RefreshTokenCookieName, tokens.RefreshToken, refreshCookieOpt);
        context.Response.Cookies.Append(_cookieSettings.AccessTokenExpireCookieName, tokens.ExpirationAccessToken.ToUnixTimeSeconds().ToString(), expiryCookieOpt);
        context.Response.Cookies.Append(_cookieSettings.SessionPresentCookieName, "true", sessionPresentOpt);
    }
}
