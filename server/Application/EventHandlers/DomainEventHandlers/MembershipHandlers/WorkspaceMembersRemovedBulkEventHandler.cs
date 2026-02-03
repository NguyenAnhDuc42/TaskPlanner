using Application.Interfaces;
using Domain.Events.Membership;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Hybrid;
using Application.Common;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

public class WorkspaceMembersRemovedBulkEventHandler : INotificationHandler<WorkspaceMembersRemovedBulkEvent>
{
    private readonly IRealtimeService _realtimeService;
    private readonly HybridCache _cache;
    private readonly ILogger<WorkspaceMembersRemovedBulkEventHandler> _logger;

    public WorkspaceMembersRemovedBulkEventHandler(
        IRealtimeService realtimeService,
        HybridCache cache,
        ILogger<WorkspaceMembersRemovedBulkEventHandler> logger)
    {
        _realtimeService = realtimeService;
        _cache = cache;
        _logger = logger;
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

        await Task.WhenAll(notificationTasks.Concat(new[] { workspaceNotifyTask }));
    }
}
