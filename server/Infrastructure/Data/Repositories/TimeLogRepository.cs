using Application.Interfaces.Repositories;
using Domain.Entities.Support;
using Infrastructure.Data;

namespace Infrastructure.Data.Repositories;

public class TimeLogRepository : BaseRepository<TimeLog>, ITimeLogRepository
{
    public TimeLogRepository(TaskPlanDbContext context) : base(context) { }
}
