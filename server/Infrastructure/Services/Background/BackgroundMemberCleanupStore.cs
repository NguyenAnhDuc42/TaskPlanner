using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Background.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services.Background;

public class BackgroundMemberCleanupStore : IBackgroundMemberCleanupStore
{
    private readonly TaskPlanDbContext _context;

    public BackgroundMemberCleanupStore(TaskPlanDbContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> GetMemberIdsForUsersAsync(Guid workspaceId, IEnumerable<Guid> userIds)
    {
        return await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(wm => userIds.Contains(wm.UserId) && wm.ProjectWorkspaceId == workspaceId)
            .Select(wm => wm.Id)
            .ToListAsync();
    }

    public async Task<(int EntityAccessDeleted, int AssignmentsDeleted)> CleanupMemberDataAsync(Guid workspaceId, List<Guid> memberIds)
    {
        var deletedAt = DateTimeOffset.UtcNow;

        var entityAccessDeleted = await _context.EntityAccesses
            .Where(ea => ea.ProjectWorkspaceId == workspaceId && memberIds.Contains(ea.WorkspaceMemberId) && ea.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.DeletedAt, deletedAt)
                .SetProperty(e => e.UpdatedAt, deletedAt));

        var assignmentsDeleted = await _context.TaskAssignments
            .Where(a => memberIds.Contains(a.WorkspaceMemberId) && a.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.DeletedAt, deletedAt)
                .SetProperty(a => a.UpdatedAt, deletedAt));

        return (entityAccessDeleted, assignmentsDeleted);
    }
}
