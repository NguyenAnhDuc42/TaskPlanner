using System;
using Microsoft.EntityFrameworkCore;
using src.Domain.Entities.SessionEntity;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly PlannerDbContext _context;
    public SessionRepository(PlannerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
    public async Task<Session?> GetSessionByRefreshTokenAsync(string refreshToken,CancellationToken cancellationToken)
    {
        return await _context.Sessions.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);
    }

    public void RemoveSessionByRefreshTokenAsync(string refreshToken)
    {
        var session = _context.Sessions.FirstOrDefault(s => s.RefreshToken == refreshToken);
        if (session is null) return;
        session.Revoke();
        _context.Sessions.Remove(session);
    }
}
