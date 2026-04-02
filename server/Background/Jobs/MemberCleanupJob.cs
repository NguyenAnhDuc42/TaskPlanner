
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

    public async Task CleanupMemberDataAsync(Guid workspaceId, Guid userId)
    {
        _logger.LogInformation("Starting cleanup for user {UserId} in workspace {WorkspaceId}", 
            userId, workspaceId);

        // Find WorkspaceMemberId
        var workspaceMemberId = await _context.WorkspaceMembers
            .AsNoTracking()
            .Where(wm => wm.UserId == userId && wm.ProjectWorkspaceId == workspaceId)
            .Select(wm => wm.Id)
            .FirstOrDefaultAsync();

        if (workspaceMemberId == Guid.Empty)
        {
            _logger.LogWarning("No WorkspaceMember found for User {UserId} in Workspace {WorkspaceId}. Skipping cleanup.", userId, workspaceId);
            return;
        }

        // Get all entity IDs in this workspace
        var spaceIds = await _context.ProjectSpaces
            .AsNoTracking()
            .Where(s => s.ProjectWorkspaceId == workspaceId)
            .Select(s => s.Id)
            .ToListAsync();

        var folderIds = await _context.ProjectFolders
            .AsNoTracking()
            .Where(f => spaceIds.Contains(f.ProjectSpaceId))
            .Select(f => f.Id)
            .ToListAsync();

        // Soft-delete EntityAccess for this user in this workspace
        var entityAccessDeleted = await _context.EntityAccesses
            .Where(ea => ea.ProjectWorkspaceId == workspaceId && ea.WorkspaceMemberId == workspaceMemberId && ea.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow));

        // Soft-delete TaskAssignments for this user in this workspace
        var taskIds = await _context.ProjectTasks
            .AsNoTracking()
            .Where(t => t.ProjectWorkspaceId == workspaceId)
            .Select(t => t.Id)
            .ToListAsync();

        var assignmentsDeleted = await _context.TaskAssignments
            .Where(a => a.WorkspaceMemberId == workspaceMemberId && taskIds.Contains(a.ProjectTaskId) && a.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.DeletedAt, DateTimeOffset.UtcNow)
                .SetProperty(a => a.UpdatedAt, DateTimeOffset.UtcNow));

        _logger.LogInformation(
            "Cleanup complete for user {UserId}: {EntityAccessCount} EntityAccess records, {Assignments} TaskAssignments",
            userId, entityAccessDeleted, assignmentsDeleted);
    }
}
