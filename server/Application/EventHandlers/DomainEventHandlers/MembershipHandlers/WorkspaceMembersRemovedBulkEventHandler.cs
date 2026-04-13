using Application.Interfaces;
using Domain.Events.Membership;
using Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Hybrid;
using Application.Common;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

public class WorkspaceMembersRemovedBulkEventHandler : IDomainEventHandler<WorkspaceMembersRemovedBulkEvent>
{
    private readonly IRealtimeService _realtimeService;
    private readonly HybridCache _cache;
    private readonly ILogger<WorkspaceMembersRemovedBulkEventHandler> _logger;
    private readonly IBackgroundJobService _backgroundJobService;

    public WorkspaceMembersRemovedBulkEventHandler(
        IRealtimeService realtimeService,
        HybridCache cache,
        ILogger<WorkspaceMembersRemovedBulkEventHandler> logger,
        IBackgroundJobService backgroundJobService)
    {
        _realtimeService = realtimeService;
        _cache = cache;
        _logger = logger;
        _backgroundJobService = backgroundJobService;
    }

    public async Task Handle(WorkspaceMembersRemovedBulkEvent notification, CancellationToken cancellationToken)
    {
        var userIds = notification.UserIds.ToList();

        _logger.LogInformation("[Plumbing] WorkspaceMembersRemovedBulkEvent for {UserCount} users in Workspace: {WorkspaceId}",
            userIds.Count, notification.WorkspaceId);

        // 2. Invalidate workspace list cache for all users (Batched)
        var invalidationTasks = userIds.Select(userId => 
            _cache.RemoveByTagAsync(CacheConstants.Tags.UserWorkspaces(userId), cancellationToken).AsTask());
        await Task.WhenAll(invalidationTasks);

        // 3. Notify users via SignalR (Concurrent)
        var notificationTasks = userIds.Select(userId => 
            _realtimeService.NotifyUserAsync(
                userId, 
                "SecurityContextChanged", 
                new { workspaceId = notification.WorkspaceId, reason = "MembershipRemoved" }, 
                cancellationToken));

        // 4. Notify the workspace about removed members
        var workspaceNotifyTask = _realtimeService.NotifyWorkspaceAsync(
            notification.WorkspaceId,
            "MembersUpdated",
            new { userIds = userIds, action = "Removed" },
            cancellationToken);

        // 5. Cleanup member data asynchronously (TODO: Use a shared interface for the cleanup job if needed)
        // For now, I'm logging that this should happen, to prevent the previous circular dependency error
        _logger.LogWarning("Background cleanup for members in workspace {WorkspaceId} should be enqueued via IBackgroundJobService", notification.WorkspaceId);

        await Task.WhenAll(notificationTasks.Concat(new[] { workspaceNotifyTask }));
    }
}
