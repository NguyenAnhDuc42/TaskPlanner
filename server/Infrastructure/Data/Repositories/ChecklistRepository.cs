using Application.Interfaces.Repositories;
using Domain.Entities.Support;
using Infrastructure.Data;

namespace Infrastructure.Data.Repositories;

public class ChecklistRepository : BaseRepository<Checklist>, IChecklistRepository
{
    public ChecklistRepository(TaskPlanDbContext context) : base(context) { }
}
