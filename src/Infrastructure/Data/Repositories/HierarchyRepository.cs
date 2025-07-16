using System;
using src.Domain.Entities.WorkspaceEntity;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public class HierarchyRepository : IHierarchyRepository
{
    private readonly PlannerDbContext _context;
    public HierarchyRepository(PlannerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PlanTask?> GetPlanTaskByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks.FindAsync(id,cancellationToken);
    }
}
