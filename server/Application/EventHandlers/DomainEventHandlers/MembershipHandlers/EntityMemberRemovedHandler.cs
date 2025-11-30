using Application.Interfaces.Services.Permissions;
using Domain.Events.Membership;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

public class EntityMemberRemovedHandler : INotificationHandler<EntityMemberRemovedEvent>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<EntityMemberRemovedHandler> _logger;

    public EntityMemberRemovedHandler(
        IPermissionService permissionService,
        ILogger<EntityMemberRemovedHandler> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task Handle(EntityMemberRemovedEvent notification, CancellationToken cancellationToken)
    {
        var evt = notification;
        _logger.LogInformation("Invalidating cache for member removal: UserId={UserId}, EntityId={EntityId}, EntityType={EntityType}",
            evt.UserId, evt.EntityId, evt.EntityType);

        await _permissionService.InvalidateEntityAccessCacheAsync(evt.UserId, evt.EntityId, evt.EntityType);
    }
}
