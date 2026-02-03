using Domain.Events.Membership;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

public class EntityMemberRemovedEventHandler : INotificationHandler<EntityMemberRemovedEvent>
{
    private readonly ILogger<EntityMemberRemovedEventHandler> _logger;

    public EntityMemberRemovedEventHandler(
        ILogger<EntityMemberRemovedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(EntityMemberRemovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invalidating cache for member removal: UserId={UserId}, EntityId={EntityId}, EntityType={EntityType}",
            notification.UserId, notification.EntityId, notification.EntityType);
    }
}
