using Microsoft.EntityFrameworkCore;

namespace Application;

// The single query for "is this user a member of this workspace, and what's their row" — used
// by WorkspaceContextMiddleware (needs the raw Status to produce a specific 403 message) and by
// WorkspaceHub/SyncHub (only ever branch on null vs non-null). Previously each of these three
// call sites hand-rolled its own independent query; this collapses them to one.
public class WorkspaceMembershipResolver(TaskPlanDbContext db)
{
    public Task<WorkspaceMember?> ResolveMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
        => db.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.ProjectWorkspaceId == workspaceId && m.UserId == userId && m.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<WorkspaceMember?> ResolveActiveMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
        => db.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.ProjectWorkspaceId == workspaceId && m.UserId == userId && m.DeletedAt == null && m.Status == MembershipStatus.Active)
            .FirstOrDefaultAsync(cancellationToken);
}
