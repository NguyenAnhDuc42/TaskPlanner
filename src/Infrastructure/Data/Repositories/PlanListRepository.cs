using System;
using Microsoft.EntityFrameworkCore;
using src.Domain.Entities.WorkspaceEntity;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public class PlanListRepository : BaseRepository<PlanList>, IPlanListRepository
{
    public PlanListRepository(PlannerDbContext context) : base(context)
    {
    }

    public async Task<PlanList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(id, cancellationToken);
    }
}
