using System;
using Microsoft.EntityFrameworkCore;
using src.Domain.Entities.WorkspaceEntity;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public class SpaceRepository : BaseRepository<Space>, ISpaceRepository
{
    private readonly PlannerDbContext _context;
    public SpaceRepository(PlannerDbContext context) : base(context)
    {
        _context = context;
    }

}
