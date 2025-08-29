using Application.Interfaces.Repositories;
using Domain.Entities.Support;
using Infrastructure.Data;

namespace Infrastructure.Data.Repositories;

public class ChecklistItemRepository : BaseRepository<ChecklistItem>, IChecklistItemRepository
{
    public ChecklistItemRepository(TaskPlanDbContext context) : base(context) { }
}
