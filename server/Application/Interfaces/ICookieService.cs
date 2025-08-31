
using Application.Features.Auth.DTOs;
using Microsoft.AspNetCore.Http;

namespace server.Application.Interfaces
{
    public interface ICookieService
    {
        void SetAuthCookies(HttpContext context, JwtTokens tokens);
        JwtTokens? GetAuthTokensFromCookies(HttpContext context);
        string GetRefreshTokenFromCookies(HttpContext context);
        void ClearAuthCookies(HttpContext context);
    }
}

