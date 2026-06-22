using Microsoft.EntityFrameworkCore;
namespace Application;

public class LogoutHandler(
    TaskPlanDbContext db, 
    CookieService cookieService
) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = cookieService.GetRefreshTokenFromCookies();
        
        cookieService.ClearAuthCookies();

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Success();

        var session = await db.Sessions.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken, cancellationToken);

        if (session != null)
        {
            session.Revoke();
            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}



