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
    private readonly ICookieService _cookieService;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RefreshTokenHandler(
        IUnitOfWork unitOfWork, 
        ICookieService cookieService, 
        ITokenService tokenService, 
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
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

        // Find session strictly by the Refresh Token cookie
        var session = await _unitOfWork.Set<Session>()
            .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken, cancellationToken)
            ?? throw new UnauthorizedAccessException("Unauthorized");

        // Validate session is active
        if (!session.IsActive) throw new UnauthorizedAccessException("Unauthorized");

        // Find the user associated with this session
        var user = await _unitOfWork.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == session.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Unauthorized");

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
