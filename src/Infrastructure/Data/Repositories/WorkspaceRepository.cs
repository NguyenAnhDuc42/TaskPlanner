using System;
using Microsoft.EntityFrameworkCore;
using src.Domain.Entities.WorkspaceEntity;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Infrastructure.Data.Repositories;

public class WorkspaceRepository : BaseRepository<Workspace>, IWorkspaceRepository
{
    private readonly PlannerDbContext _context;
    public WorkspaceRepository(PlannerDbContext context) : base(context)
    {
        _context = context;
    }


    public async Task<Workspace?> GetWithMembersByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<Workspace?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(w => w.JoinCode == joinCode, cancellationToken);
    }

    public async Task<Guid?> GetUserWorkspaceAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default)
    {
        var workspaceMember = await Context.UserWorkspaces
            .FirstOrDefaultAsync(wm => wm.UserId == userId && wm.WorkspaceId == workspaceId, cancellationToken);

        return workspaceMember?.WorkspaceId;
    }
}
