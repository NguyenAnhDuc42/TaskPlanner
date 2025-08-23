using Application.Common.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;

namespace Infrastructure.Data.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(TaskPlanDbContext context) : base(context)
        {
        }
    }
}