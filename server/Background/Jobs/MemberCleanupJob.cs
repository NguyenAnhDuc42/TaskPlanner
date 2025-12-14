using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
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

        var listIds = await _context.ProjectLists
            .AsNoTracking()
            .Where(l => spaceIds.Contains(l.ProjectSpaceId))
            .Select(l => l.Id)
            .ToListAsync();

        var allLayerIds = spaceIds.Concat(folderIds).Concat(listIds).ToList();

        // Soft-delete EntityMembers for this user
        var entityMembersDeleted = await _context.EntityMembers
            .Where(em => em.UserId == userId && allLayerIds.Contains(em.LayerId) && em.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow));

        // Soft-delete TaskAssignments for this user
        var taskIds = await _context.ProjectTasks
            .AsNoTracking()
            .Where(t => listIds.Contains(t.ProjectListId))
            .Select(t => t.Id)
            .ToListAsync();

        var assignmentsDeleted = await _context.TaskAssignments
            .Where(a => a.AssigneeId == userId && taskIds.Contains(a.TaskId) && a.DeletedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.DeletedAt, DateTimeOffset.UtcNow)
                .SetProperty(a => a.UpdatedAt, DateTimeOffset.UtcNow));

        _logger.LogInformation(
            "Cleanup complete for user {UserId}: {EntityMembers} EntityMembers, {Assignments} TaskAssignments",
            userId, entityMembersDeleted, assignmentsDeleted);
    }
}
