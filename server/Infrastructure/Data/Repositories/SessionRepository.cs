using Application.Common.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;

namespace Infrastructure.Data.Repositories
{
    public class SessionRepository : BaseRepository<Session>, ISessionRepository
    {
        public SessionRepository(TaskPlanDbContext context) : base(context)
        {
        }
    }
}