using Application.Interfaces.Services.Permissions;
using Domain.Events.Membership;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

public class EntityMemberAccessChangedHandler : INotificationHandler<EntityMemberAccessChangedEvent>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<EntityMemberAccessChangedHandler> _logger;

    public EntityMemberAccessChangedHandler(
        IPermissionService permissionService,
        ILogger<EntityMemberAccessChangedHandler> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task Handle(EntityMemberAccessChangedEvent notification, CancellationToken cancellationToken)
    {
        var evt = notification.Event;
        _logger.LogInformation("Invalidating cache for access change: UserId={UserId}, EntityId={EntityId}, EntityType={EntityType}, {OldAccess}->{NewAccess}",
            evt.UserId, evt.EntityId, evt.EntityType, evt.OldAccess, evt.NewAccess);

        await _permissionService.InvalidateEntityAccessCacheAsync(evt.UserId, evt.EntityId, evt.EntityType);
    }
}
