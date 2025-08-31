using System;
using MediatR;
using Microsoft.AspNetCore.Http;
using server.Application.Interfaces;

namespace Application.Features.Auth.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly ICookieService _cookieService;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RefreshTokenHandler(ICookieService cookieService, ITokenService tokenService, IHttpContextAccessor httpContextAccessor)
    {
        _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }
    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) throw new Exception("Unable to get HttpContext from IHttpContextAccessor.");
        var refreshToken = _cookieService.GetRefreshTokenFromCookies(httpContext);
        if (string.IsNullOrEmpty(refreshToken)) throw new UnauthorizedAccessException("Unauthorized");
        var tokens = await _tokenService.RefreshAccessTokenAsync(refreshToken, cancellationToken);
        if (tokens is null) throw new UnauthorizedAccessException("Unauthorized");
        _cookieService.SetAuthCookies(httpContext, tokens);
        return new RefreshTokenResponse(tokens.ExpirationAccessToken, tokens.ExpirationRefreshToken);
    }
}
