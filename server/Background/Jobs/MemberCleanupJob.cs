using Background.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Background.Jobs;

public class MemberCleanupJob
{
    private readonly IBackgroundMemberCleanupStore _store;
    private readonly ILogger<MemberCleanupJob> _logger;

    public MemberCleanupJob(IBackgroundMemberCleanupStore store, ILogger<MemberCleanupJob> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task CleanupMembersDataAsync(Guid workspaceId, IEnumerable<Guid> userIds)
    {
        var userList = userIds.ToList();
        if (!userList.Any()) return;

        _logger.LogInformation("Starting bulk cleanup for {Count} users in workspace {WorkspaceId}", 
            userList.Count, workspaceId);

        // Find all WorkspaceMemberIds for these users via store
        var memberIds = await _store.GetMemberIdsForUsersAsync(workspaceId, userList);

        if (!memberIds.Any())
        {
            _logger.LogWarning("No WorkspaceMembers found for the provided users in Workspace {WorkspaceId}. Skipping cleanup.", workspaceId);
            return;
        }

        // Perform cleanup via store
        var (entityAccessDeleted, assignmentsDeleted) = await _store.CleanupMemberDataAsync(workspaceId, memberIds);

        _logger.LogInformation(
            "Bulk cleanup complete for workspace {WorkspaceId}: {EntityAccessCount} EntityAccess records, {Assignments} TaskAssignments",
            workspaceId, entityAccessDeleted, assignmentsDeleted);
    }
}
