using Domain.Events.Membership;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

public class EntityAccessRemovedEventHandler : INotificationHandler<EntityAccessRemovedEvent>
{
    private readonly ILogger<EntityAccessRemovedEventHandler> _logger;

    public EntityAccessRemovedEventHandler(
        ILogger<EntityAccessRemovedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(EntityAccessRemovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invalidating cache for member removal: UserId={UserId}, EntityId={EntityId}, EntityType={EntityType}",
            notification.UserId, notification.EntityId, notification.EntityType);
    }
}
