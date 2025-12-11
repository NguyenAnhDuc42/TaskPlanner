using System;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using server.Application.Interfaces;

namespace Application.Features.Auth.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICookieService _cookieService;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RefreshTokenHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, ICookieService cookieService, ITokenService tokenService, IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }
    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) throw new Exception("Unable to get HttpContext from IHttpContextAccessor.");
        var user = _currentUserService.CurrentUserWithSession();

        var refreshToken = _cookieService.GetRefreshTokenFromCookies(httpContext);
        if (string.IsNullOrEmpty(refreshToken)) throw new UnauthorizedAccessException("Unauthorized");
        
        var tokens = await _tokenService.RefreshAccessTokenAsync(refreshToken, cancellationToken);
        if (tokens is null) throw new UnauthorizedAccessException("Unauthorized");

        user.ExtendSession(refreshToken, tokens.ExpirationRefreshToken - DateTimeOffset.UtcNow);
        _unitOfWork.Set<User>().Update(user);
        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult <= 0) throw new UnauthorizedAccessException("Unauthorized");

        _cookieService.SetAuthCookies(httpContext, tokens);

        return new RefreshTokenResponse(tokens.ExpirationAccessToken, tokens.ExpirationRefreshToken);
    }
}
