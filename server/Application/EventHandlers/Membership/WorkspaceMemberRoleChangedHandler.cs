using Application.Interfaces.Services.Permissions;
using Domain.Events.Membership;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.Membership;

public class WorkspaceMemberRoleChangedHandler : INotificationHandler<WorkspaceMemberRoleChangedEvent>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<WorkspaceMemberRoleChangedHandler> _logger;

    public WorkspaceMemberRoleChangedHandler(
        IPermissionService permissionService,
        ILogger<WorkspaceMemberRoleChangedHandler> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task Handle(WorkspaceMemberRoleChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invalidating cache for role change: UserId={UserId}, WorkspaceId={WorkspaceId}, {OldRole}->{NewRole}",
            notification.UserId, notification.WorkspaceId, notification.OldRole, notification.NewRole);

        await _permissionService.InvalidateWorkspaceRoleCacheAsync(
            notification.UserId,
            notification.WorkspaceId);
    }
}
