using Application.Common.Interfaces.Repositories;
using Domain.Entities.Support;
using Infrastructure.Data;
using Infrastructure.Data.Repositories;

namespace Infrastructure.Data.Repositories
{
    public class ChecklistRepository : BaseRepository<Checklist>, IChecklistRepository
    {
        public ChecklistRepository(TaskPlanDbContext context) : base(context)
        {
        }
    }
}