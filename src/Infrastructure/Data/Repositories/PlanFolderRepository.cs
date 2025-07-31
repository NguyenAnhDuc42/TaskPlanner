using System;
using Microsoft.EntityFrameworkCore;
using src.Domain.Entities.WorkspaceEntity;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public class PlanFolderRepository : BaseRepository<PlanFolder>, IPlanFolderRepository
{
    private readonly PlannerDbContext _context;
    public PlanFolderRepository(PlannerDbContext context) : base(context)
    {
        _context = context;
    }

}
