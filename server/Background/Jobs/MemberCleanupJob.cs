
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Background.Jobs;

/// <summary>
/// Background job for cleaning up member data when they leave a workspace.
/// Soft-deletes EntityMembers, TaskAssignments, etc.
/// </summary>
public class MemberCleanupJob
{
    private readonly TaskPlanDbContext _context;
    private readonly ILogger<MemberCleanupJob> _logger;

    public MemberCleanupJob(TaskPlanDbContext context, ILogger<MemberCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CleanupMembersDataAsync(Guid workspaceId, IEnumerable<Guid> userIds)
    {
        var userList = userIds.ToList();
        if (!userList.Any()) return;

        _logger.LogInformation("Starting bulk cleanup for {Count} users in workspace {WorkspaceId}", 
            userList.Count, workspaceId);

        // Find all WorkspaceMemberIds for these users
        var memberIds = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(wm => userList.Contains(wm.UserId) && wm.ProjectWorkspaceId == workspaceId)
            .Select(wm => wm.Id)
            .ToListAsync();

        if (!memberIds.Any())
        {
            _logger.LogWarning("No WorkspaceMembers found for the provided users in Workspace {WorkspaceId}. Skipping cleanup.", workspaceId);
            return;
        }

        var deletedAt = DateTimeOffset.UtcNow;

        // 1. Soft-delete EntityAccess for these members
        var entityAccessDeleted = await _context.EntityAccesses
            .Where(ea => ea.ProjectWorkspaceId == workspaceId && memberIds.Contains(ea.WorkspaceMemberId) && ea.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.DeletedAt, deletedAt)
                .SetProperty(e => e.UpdatedAt, deletedAt));

        // 2. Soft-delete TaskAssignments for these members
        var assignmentsDeleted = await _context.TaskAssignments
            .Where(a => memberIds.Contains(a.WorkspaceMemberId) && a.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.DeletedAt, deletedAt)
                .SetProperty(a => a.UpdatedAt, deletedAt));

        _logger.LogInformation(
            "Bulk cleanup complete for workspace {WorkspaceId}: {EntityAccessCount} EntityAccess records, {Assignments} TaskAssignments",
            workspaceId, entityAccessDeleted, assignmentsDeleted);
    }
}
