using Application.Interfaces;
using Application.Interfaces.Services.Permissions;
using Domain.Events.Membership;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

public class EntityMemberAccessChangedEventHandler : INotificationHandler<EntityMemberAccessChangedEvent>
{
    private readonly IPermissionService _permissionService;
    private readonly IRealtimeService _realtimeService;
    private readonly ILogger<EntityMemberAccessChangedEventHandler> _logger;

    public EntityMemberAccessChangedEventHandler(
        IPermissionService permissionService,
        IRealtimeService realtimeService,
        ILogger<EntityMemberAccessChangedEventHandler> logger)
    {
        _permissionService = permissionService;
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task Handle(EntityMemberAccessChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Plumbing] Invalidating cache for access change: UserId={UserId}, EntityId={EntityId}, EntityType={EntityType}, {OldAccess}->{NewAccess}",
            notification.UserId, notification.EntityId, notification.EntityType, notification.OldAccess, notification.NewAccess);

        // 1. Invalidate cache (Plumbing)
        await _permissionService.InvalidateEntityAccessCacheAsync(notification.UserId, notification.EntityId, notification.EntityType);

        // 2. Notify user (UI Plumbing)
        await _realtimeService.NotifyUserAsync(
            notification.UserId, 
            "SecurityContextChanged", 
            new { entityId = notification.EntityId, entityType = notification.EntityType, reason = "AccessChanged" }, 
            cancellationToken);
    }
}
