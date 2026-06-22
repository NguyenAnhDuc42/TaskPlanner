using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Application;

public class RefreshTokenHandler(
    TaskPlanDbContext db,
    CookieService cookieService,
    TokenService tokenService,
    ILogger<RefreshTokenHandler> logger
) : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = cookieService.GetRefreshTokenFromCookies();
        if (string.IsNullOrEmpty(refreshToken))
        {
            cookieService.ClearAuthCookies();
            return Result<RefreshTokenResponse>.Failure(AuthError.InvalidSession);
        }

        var session = await db.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken, cancellationToken);

        if (session is null)
        {
            // Token not found as current — check if it was already rotated away (reuse attack)
            var compromised = await db.Sessions
                .FirstOrDefaultAsync(s => s.PreviousRefreshToken == refreshToken && !s.RevokedAt.HasValue, cancellationToken);

            if (compromised is not null)
            {
                logger.LogWarning("Refresh token reuse detected for session {SessionId}. Revoking session.", compromised.Id);
                compromised.Revoke();
                await db.SaveChangesAsync(cancellationToken);
            }

            cookieService.ClearAuthCookies();
            return Result<RefreshTokenResponse>.Failure(AuthError.InvalidSession);
        }

        if (!session.IsActive || session.User is null)
        {
            cookieService.ClearAuthCookies();
            return Result<RefreshTokenResponse>.Failure(AuthError.InvalidSession);
        }

        // 1. Rotate tokens
        var tokens = tokenService.RefreshAccessToken(session, session.User);

        // 2. Extend session expiry
        session.ExtendExpiration(tokenService.GetRefreshTokenDuration());
        await db.SaveChangesAsync(cancellationToken);

        // 3. Set new cookies
        cookieService.SetAuthCookies(tokens);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(
            tokens.ExpirationAccessToken,
            tokens.ExpirationRefreshToken
        ));
    }
}



