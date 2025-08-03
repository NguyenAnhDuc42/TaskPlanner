using System;
using MediatR;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IServices;

namespace src.Feature.User.Auth.RefreshToken;

public class RefreshTokenRequestHandler : IRequestHandler<RefreshTokenRequest, Result<RefreshTokenResponse, ErrorResponse>>
{
    private readonly ICookieService _cookieService;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public RefreshTokenRequestHandler(ICookieService cookieService, ITokenService tokenService, IHttpContextAccessor httpContextAccessor)
    {
        _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }
    public async Task<Result<RefreshTokenResponse, ErrorResponse>> Handle(RefreshTokenRequest request, CancellationToken cancellationToken)
    {

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return Result<RefreshTokenResponse, ErrorResponse>.Failure(ErrorResponse.Internal("An unexpected error occurred."));

        var refreshToken = _cookieService.GetRefreshTokenFromCookies(httpContext);
        if (string.IsNullOrEmpty(refreshToken)) return Result<RefreshTokenResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized"));

        var tokens = await _tokenService.RefreshAccessTokenAsync(refreshToken, cancellationToken);
        if (tokens is null) return Result<RefreshTokenResponse, ErrorResponse>.Failure(ErrorResponse.Unauthorized("Unauthorized"));
        _cookieService.SetAuthCookies(httpContext, tokens);
        return Result<RefreshTokenResponse, ErrorResponse>.Success(new RefreshTokenResponse(tokens.ExpirationAccessToken, tokens.ExpirationRefreshToken));

    }
}
