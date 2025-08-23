using Application.Common.Interfaces.Repositories;
using Domain.Entities.Support;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;

namespace Infrastructure.Data.Repositories
{
    public class StatusRepository : BaseRepository<Status>, IStatusRepository
    {
        public StatusRepository(TaskPlanDbContext context) : base(context)
        {
        }
    }
}