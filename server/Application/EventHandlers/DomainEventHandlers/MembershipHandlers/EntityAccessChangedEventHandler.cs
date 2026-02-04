using Application.Interfaces;
using Domain.Events.Membership;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

public class EntityAccessChangedEventHandler : INotificationHandler<EntityAccessChangedEvent>
{
    private readonly IRealtimeService _realtimeService;
    private readonly ILogger<EntityAccessChangedEventHandler> _logger;

    public EntityAccessChangedEventHandler(
        IRealtimeService realtimeService,
        ILogger<EntityAccessChangedEventHandler> logger)
    {
        _realtimeService = realtimeService;
        _logger = logger;
    }

    public async Task Handle(EntityAccessChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Plumbing] Invalidating cache for access change: UserId={UserId}, EntityId={EntityId}, EntityType={EntityType}, {OldAccess}->{NewAccess}",
            notification.UserId, notification.EntityId, notification.EntityType, notification.OldAccess, notification.NewAccess);

        // 2. Notify user (UI Plumbing)
        await _realtimeService.NotifyUserAsync(
            notification.UserId, 
            "SecurityContextChanged", 
            new { entityId = notification.EntityId, entityType = notification.EntityType, reason = "AccessChanged" }, 
            cancellationToken);
    }
}
