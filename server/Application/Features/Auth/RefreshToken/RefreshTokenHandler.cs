using System;
using Application.Interfaces.Repositories;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.Auth.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICookieService _cookieService;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RefreshTokenHandler(
        IUnitOfWork unitOfWork, 
        ICurrentUserService currentUserService, 
        ICookieService cookieService, 
        ITokenService tokenService, 
        IHttpContextAccessor httpContextAccessor)
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
        var user = _currentUserService.CurrentUser();

        var refreshToken = _cookieService.GetRefreshTokenFromCookies(httpContext);
        if (string.IsNullOrEmpty(refreshToken)) throw new UnauthorizedAccessException("Unauthorized");

        // Find session by refresh token
        var session = await _unitOfWork.Set<Session>()
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.RefreshToken == refreshToken, cancellationToken)
            ?? throw new UnauthorizedAccessException("Unauthorized");

        // Validate session is active
        if (session.RevokedAt.HasValue) throw new UnauthorizedAccessException("Session revoked");
        if (session.ExpiresAt < DateTimeOffset.UtcNow) throw new UnauthorizedAccessException("Session expired");

        var tokens = _tokenService.RefreshAccessToken(session, user);

        // Extend session by configured duration
        var refreshDuration = _tokenService.GetRefreshTokenDuration();
        session.ExtendExpiration(refreshDuration);
        
        _unitOfWork.Set<Session>().Update(session);
        var saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (saveResult <= 0) throw new UnauthorizedAccessException("Unauthorized");

        _cookieService.SetAuthCookies(httpContext, tokens);

        return new RefreshTokenResponse(tokens.ExpirationAccessToken, tokens.ExpirationRefreshToken);
    }
}
