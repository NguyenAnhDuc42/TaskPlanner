using Application.Interfaces.Data;
using Application.Common.Results;
using Application.Common.Errors;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;

namespace Application.Features.Auth;

public class RefreshTokenHandler(
    IDataBase db, 
    ICookieService cookieService, 
    ITokenService tokenService
) : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var refreshToken = cookieService.GetRefreshTokenFromCookies();
        if (string.IsNullOrEmpty(refreshToken))
        {
            cookieService.ClearAuthCookies();
            return Result<RefreshTokenResponse>.Failure(AuthError.InvalidSession);
        }

        var session = await db.Sessions
            .ByRefreshToken(refreshToken)
            .Include(s => s.User)
            .FirstOrDefaultAsync(ct);
        
        if (session is null || !session.IsActive || session.User is null) 
        {
            cookieService.ClearAuthCookies();
            return Result<RefreshTokenResponse>.Failure(AuthError.InvalidSession);
        }

        // 1. Refresh Tokens (Rotation)
        var tokens = tokenService.RefreshAccessToken(session, session.User);

        // 2. Extend/Update Session
        session.ExtendExpiration(tokenService.GetRefreshTokenDuration());
        await db.SaveChangesAsync(ct);
        
        // 3. Update Cookies
        cookieService.SetAuthCookies(tokens);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(
            tokens.ExpirationAccessToken, 
            tokens.ExpirationRefreshToken
        ));
    }
}
