using System;
using Microsoft.EntityFrameworkCore;
using src.Domain.Entities.SessionEntity;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public class SessionRepository :BaseRepository<Session>, ISessionRepository
{

    public SessionRepository(PlannerDbContext context) : base(context){}
    public async Task<Session?> GetSessionByRefreshTokenAsync(string refreshToken,CancellationToken cancellationToken)
    {
        return await Context.Sessions.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);
    }

    public void RemoveSessionByRefreshTokenAsync(string refreshToken)
    {
        var session = Context.Sessions.FirstOrDefault(s => s.RefreshToken == refreshToken);
        if (session is null) return;
        session.Revoke();
        Context.Sessions.Remove(session);
    }
}
