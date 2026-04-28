using Application.Interfaces.Data;
using Application.Common.Results;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;
using Domain.Entities;

namespace Application.Features.Auth;

public class LogoutHandler(
    IDataBase db, 
    ICookieService cookieService
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
