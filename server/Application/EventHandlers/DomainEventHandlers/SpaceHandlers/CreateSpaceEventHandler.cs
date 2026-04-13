using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Events.Space;
using Application.Common.Interfaces;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Domain.Enums.RelationShip;
using Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.EventHandlers.DomainEventHandlers.SpaceHandlers;

public class CreateSpaceEventHandler(
    ILogger<CreateSpaceEventHandler> logger, 
    IDataBase db, 
    IRealtimeService realtime, 
    HybridCache cache
) : IDomainEventHandler<SpaceCreatedEvent>
{
    public async Task Handle(SpaceCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding default views (Overview & Tasks) for SpaceId: {SpaceId}", notification.SpaceId);

        var overviewView = ViewDefinition.Create(
            notification.SpaceId,
            EntityLayerType.ProjectSpace,
            "Overview",
            ViewType.Overview,
            notification.UserId,
            isDefault: true
        );

        var tasksView = ViewDefinition.Create(
            notification.SpaceId,
            EntityLayerType.ProjectSpace,
            "Tasks",
            ViewType.Tasks,
            notification.UserId,
            isDefault: true
        );

        await db.ViewDefinitions.AddRangeAsync(new[] { overviewView, tasksView }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // STAGE 2 Notification: Background seeding is complete
        await realtime.NotifyWorkspaceAsync(notification.WorkspaceId, "SpaceReady", new { SpaceId = notification.SpaceId, WorkspaceId = notification.WorkspaceId }, cancellationToken);
    }
}
