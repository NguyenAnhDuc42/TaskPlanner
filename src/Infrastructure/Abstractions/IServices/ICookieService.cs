using System;
using src.Infrastructure.Services;

namespace src.Infrastructure.Abstractions.IServices;

public interface ICookieService
{
    void SetAuthCookies(HttpContext context, JwtTokens tokens);
    JwtTokens? GetAuthTokensFromCookies(HttpContext context);
    string GetRefreshTokenFromCookies(HttpContext context);
    void ClearAuthCookies(HttpContext context);
}
