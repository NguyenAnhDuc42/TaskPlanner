using Application.Interfaces.Services.Permissions;
using Domain.Events.Membership;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

public class EntityMemberRemovedEventHandler : INotificationHandler<EntityMemberRemovedEvent>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<EntityMemberRemovedEventHandler> _logger;

    public EntityMemberRemovedEventHandler(
        IPermissionService permissionService,
        ILogger<EntityMemberRemovedEventHandler> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task Handle(EntityMemberRemovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invalidating cache for member removal: UserId={UserId}, EntityId={EntityId}, EntityType={EntityType}",
            notification.UserId, notification.EntityId, notification.EntityType);

        await _permissionService.InvalidateEntityAccessCacheAsync(notification.UserId, notification.EntityId, notification.EntityType);
    }
}
