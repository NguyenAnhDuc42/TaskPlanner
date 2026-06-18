using Microsoft.EntityFrameworkCore;
namespace Application;

public class RefreshTokenHandler(
    TaskPlanDbContext db, 
    CookieService cookieService, 
    TokenService tokenService
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
        
        if (session is null || !session.IsActive || session.User is null) 
        {
            cookieService.ClearAuthCookies();
            return Result<RefreshTokenResponse>.Failure(AuthError.InvalidSession);
        }

        // 1. Refresh Tokens (Rotation)
        var tokens = tokenService.RefreshAccessToken(session, session.User);

        // 2. Extend/Update Session
        session.ExtendExpiration(tokenService.GetRefreshTokenDuration());
        await db.SaveChangesAsync(cancellationToken);
        
        // 3. Update Cookies
        cookieService.SetAuthCookies(tokens);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse(
            tokens.ExpirationAccessToken, 
            tokens.ExpirationRefreshToken
        ));
    }
}



