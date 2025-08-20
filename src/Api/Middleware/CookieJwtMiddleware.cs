using System;
using src.Infrastructure.Abstractions.IServices;
using Microsoft.AspNetCore.Http; // Add this using directive for HttpContext and RequestDelegate
using System.Threading.Tasks; // Add this using directive for Task

namespace src.Api.Middleware; // Updated namespace

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