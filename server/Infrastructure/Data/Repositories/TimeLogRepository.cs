using Application.Common.Interfaces.Repositories;
using Domain.Entities.Support;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;

namespace Infrastructure.Data.Repositories
{
    public class TimeLogRepository : BaseRepository<TimeLog>, ITimeLogRepository
    {
        public TimeLogRepository(TaskPlanDbContext context) : base(context)
        {
        }
    }
}