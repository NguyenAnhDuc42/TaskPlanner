using Application.Interfaces.Services.Permissions;
using Domain.Events.Membership;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

public class EntityMemberAccessChangedEventHandler : INotificationHandler<EntityMemberAccessChangedEvent>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<EntityMemberAccessChangedEventHandler> _logger;

    public EntityMemberAccessChangedEventHandler(
        IPermissionService permissionService,
        ILogger<EntityMemberAccessChangedEventHandler> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task Handle(EntityMemberAccessChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invalidating cache for access change: UserId={UserId}, EntityId={EntityId}, EntityType={EntityType}, {OldAccess}->{NewAccess}",
            notification.UserId, notification.EntityId, notification.EntityType, notification.OldAccess, notification.NewAccess);

        await _permissionService.InvalidateEntityAccessCacheAsync(notification.UserId, notification.EntityId, notification.EntityType);
    }
}
