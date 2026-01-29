using Application.Interfaces;
using Application.Interfaces.Services.Permissions;
using Domain.Events.Membership;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

public class WorkspaceMemberRoleChangedEventHandler : INotificationHandler<WorkspaceMemberRoleChangedEvent>
{
    private readonly IPermissionService _permissionService;
    private readonly IRealtimeService _realtimeService;
    private readonly ILogger<WorkspaceMemberRoleChangedEventHandler> _logger;

    public WorkspaceMemberRoleChangedEventHandler(
        IPermissionService permissionService,
        IRealtimeService realtimeService,
        ILogger<WorkspaceMemberRoleChangedEventHandler> logger)
    {
        _permissionService = permissionService;
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task Handle(WorkspaceMemberRoleChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Plumbing] Invalidate cache for role change: UserId={UserId}, WorkspaceId={WorkspaceId}, {OldRole}->{NewRole}",
            notification.UserId, notification.WorkspaceId, notification.OldRole, notification.NewRole);

        // 1. Invalidate cache (Plumbing)
        await _permissionService.InvalidateWorkspaceRoleCacheAsync(notification.UserId, notification.WorkspaceId);

        // 2. Notify user (UI Plumbing)
        await _realtimeService.NotifyUserAsync(
            notification.UserId, 
            "SecurityContextChanged", 
            new { workspaceId = notification.WorkspaceId, reason = "RoleChanged" }, 
            cancellationToken);
    }
}
