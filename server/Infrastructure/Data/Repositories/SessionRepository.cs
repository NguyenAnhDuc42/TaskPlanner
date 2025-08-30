using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Repositories;

public class SessionRepository : BaseRepository<Session>, ISessionRepository
{
    public SessionRepository(TaskPlanDbContext context) : base(context) { }

    public Task<Session?> GetByRefreshToken(string refreshToken, CancellationToken cancellationToken = default)
    {
        return _context.Sessions.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken,cancellationToken);
    }

}
