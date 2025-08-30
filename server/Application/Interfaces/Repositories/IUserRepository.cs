
using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetUsersByEmail(string email,CancellationToken cancellationToken = default);
    }
}
