using Microsoft.EntityFrameworkCore;
namespace Application;

public class LogoutHandler(
    TaskPlanDbContext db, 
    CookieService cookieService
) : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        var refreshToken = cookieService.GetRefreshTokenFromCookies();
        
        cookieService.ClearAuthCookies();

        if (string.IsNullOrWhiteSpace(refreshToken))
            return Result.Success();

        var session = await db.Sessions.ByRefreshToken(refreshToken).FirstOrDefaultAsync(ct);

        if (session != null)
        {
            db.Sessions.Remove(session); 
            await db.SaveChangesAsync(ct);
        }

        return Result.Success();
    }
}



