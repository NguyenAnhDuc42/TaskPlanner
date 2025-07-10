using System;
using src.Infrastructure.Abstractions.IServices;

namespace src.Helper.Middleware;

public class CookieJwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICookieService _cookieService;
    private readonly ITokenService _tokenService;
    public CookieJwtMiddleware(RequestDelegate next, ICookieService cookieService, ITokenService tokenService)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            var tokens = _cookieService.GetAuthTokensFromCookies(context);
            if (tokens != null && !string.IsNullOrEmpty(tokens.AccessToken))
            {
                var principal = _tokenService.ValidateToken(tokens.AccessToken);
                if (principal != null)
                {
                    context.User = principal;
                    context.Request.Headers["Authorization"] = $"Bearer {tokens.AccessToken}";
                }
            }
        }   
        await _next(context);
    }
 
}
