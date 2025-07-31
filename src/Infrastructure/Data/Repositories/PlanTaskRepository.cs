using System;
using Microsoft.EntityFrameworkCore;
using src.Domain.Entities.WorkspaceEntity;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public class PlanTaskRepository : BaseRepository<PlanTask>, IPlanTaskRepository
{
    private readonly PlannerDbContext _context;
    public PlanTaskRepository(PlannerDbContext context) : base(context)
    {
         _context = context;
    }

    public async Task<PlanTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(id, cancellationToken);
    }

    public async Task<PlanTask?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(e => e.Id == id && e.CreatorId == userId, cancellationToken);
    }

    public async Task<bool> IsOwnedByUser(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
         return await DbSet
            .AsNoTracking()
            .AnyAsync(t => t.Id == id && t.CreatorId == userId, cancellationToken);
    }
}
