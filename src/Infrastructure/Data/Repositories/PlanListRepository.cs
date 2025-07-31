using System;
using Microsoft.EntityFrameworkCore;
using src.Domain.Entities.WorkspaceEntity;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public class PlanListRepository : BaseRepository<PlanList>, IPlanListRepository
{
    private readonly PlannerDbContext _context;
    public PlanListRepository(PlannerDbContext context) : base(context)
    {
        _context = context;
    }


}
