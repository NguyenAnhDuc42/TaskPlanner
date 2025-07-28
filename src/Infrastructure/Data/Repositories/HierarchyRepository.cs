using System;
using Microsoft.EntityFrameworkCore;
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

    public async Task<PlanFolder?> GetPlanFolderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Folders.FirstOrDefaultAsync(folder => folder.Id == id, cancellationToken);
    }

    public async Task<PlanList?> GetPlanListByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
       return await _context.Lists.FirstOrDefaultAsync(list => list.Id == id, cancellationToken);
    }

    public async Task<PlanTask?> GetPlanTaskByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks.FindAsync(id,cancellationToken);
    }

    public async Task<PlanTask?> GetPlanTaskByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks.FirstOrDefaultAsync(task => task.Id == id && task.CreatorId == userId, cancellationToken);
    }

    public async Task<Space?> GetSpaceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Spaces.FirstOrDefaultAsync(space => space.Id == id, cancellationToken);
    }

    public async Task<Guid?> GetUserWorkspaceAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await _context.UserWorkspaces.Where(uw => uw.UserId == userId && uw.WorkspaceId == workspaceId).
            Select(uw => uw.UserId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Workspace?> GetWorkspaceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Workspaces.FirstOrDefaultAsync(workspace => workspace.Id == id, cancellationToken);
    }

    public async Task<Workspace?> GetWorkspaceByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
    {
        return await _context.Workspaces.FirstOrDefaultAsync(workspace => workspace.JoinCode == joinCode, cancellationToken);
    }

    public async Task<Workspace?> GetWorkspaceWithMembersByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<bool> IsOwnedByUser(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Tasks.AnyAsync(task => task.Id == id && task.CreatorId == userId, cancellationToken);
    }
}
