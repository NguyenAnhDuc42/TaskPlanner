
using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface ISessionRepository : IBaseRepository<Session>
    {
        Task<Session?> GetByRefreshToken(string refreshToken,CancellationToken cancellationToken = default);
    }
}
