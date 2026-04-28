using Application.Features.Auth;

namespace Application.Interfaces;

public interface ICookieService
{
    void SetAuthCookies(JwtTokens tokens);
    void ClearAuthCookies();
    string? GetRefreshTokenFromCookies();
    JwtTokens? GetAuthTokensFromCookies();
}
