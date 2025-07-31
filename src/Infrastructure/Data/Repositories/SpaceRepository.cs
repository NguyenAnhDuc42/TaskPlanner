using System;
using Microsoft.EntityFrameworkCore;
using src.Domain.Entities.WorkspaceEntity;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public class SpaceRepository : BaseRepository<Space>, ISpaceRepository
{
    public SpaceRepository(PlannerDbContext context) : base(context)
    {
    }

    public async Task<Space?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(id, cancellationToken);
    }
}
