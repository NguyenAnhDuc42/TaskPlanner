using Application.Interfaces;
using Application.Interfaces.Services.Permissions;
using Domain.Events.Membership;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Hybrid;
using Application.Common;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

public class WorkspaceMembersAddedBulkEventHandler : INotificationHandler<WorkspaceMembersAddedBulkEvent>
{
    private readonly IPermissionService _permissionService;
    private readonly IRealtimeService _realtimeService;
    private readonly HybridCache _cache;
    private readonly ILogger<WorkspaceMembersAddedBulkEventHandler> _logger;

    public WorkspaceMembersAddedBulkEventHandler(
        IPermissionService permissionService,
        IRealtimeService realtimeService,
        HybridCache cache,
        ILogger<WorkspaceMembersAddedBulkEventHandler> logger)
    {
        _permissionService = permissionService;
        _realtimeService = realtimeService;
        _cache = cache;
        _logger = logger;
    }

    public async Task Handle(WorkspaceMembersAddedBulkEvent notification, CancellationToken cancellationToken)
    {
        var userIds = notification.AddedMembers.Select(m => m.UserId).ToList();
        
        _logger.LogInformation("[Plumbing] WorkspaceMembersAddedBulkEvent for {UserCount} users in Workspace: {WorkspaceId}",
            userIds.Count, notification.WorkspaceId);

        // 1. Invalidate permission cache (Batched)
        await _permissionService.InvalidateBulkUserCacheAsync(notification.WorkspaceId, userIds);

        // 2. Invalidate workspace list cache for all users (Batched)
        var invalidationTasks = userIds.Select(userId => 
            _cache.RemoveByTagAsync(CacheConstants.Tags.UserWorkspaces(userId), cancellationToken).AsTask());
        await Task.WhenAll(invalidationTasks);

        // 3. Notify users via SignalR (Concurrent)
        var notificationTasks = notification.AddedMembers.Select(m => 
            _realtimeService.NotifyUserAsync(
                m.UserId, 
                "SecurityContextChanged", 
                new { workspaceId = notification.WorkspaceId, reason = "MembershipAdded" }, 
                cancellationToken));
        
        // 4. Notify the workspace about new members
        var workspaceNotifyTask = _realtimeService.NotifyWorkspaceAsync(
            notification.WorkspaceId,
            "MembersUpdated",
            new { members = notification.AddedMembers.Select(m => new { userId = m.UserId, role = m.Role }), action = "Added" },
            cancellationToken);

        await Task.WhenAll(notificationTasks.Concat(new[] { workspaceNotifyTask }));
    }
}
