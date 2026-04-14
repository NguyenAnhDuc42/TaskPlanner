using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Events.Folder;
using Application.Common.Interfaces;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Domain.Enums.RelationShip;
using Domain.Enums;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.EventHandlers.DomainEventHandlers.FolderHandlers;

public class CreateFolderEventHandler(
    ILogger<CreateFolderEventHandler> logger, 
    IDataBase db, 
    IRealtimeService realtime
) : IDomainEventHandler<FolderCreatedEvent>
{
    public async Task Handle(FolderCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Seeding default views (Overview & Tasks) for FolderId: {FolderId}", notification.FolderId);

        var overviewView = ViewDefinition.Create(
            notification.WorkspaceId,
            notification.FolderId,
            EntityLayerType.ProjectFolder,
            "Overview",
            ViewType.Overview,
            notification.UserId,
            isDefault: true
        );

        var tasksView = ViewDefinition.Create(
            notification.WorkspaceId,
            notification.FolderId,
            EntityLayerType.ProjectFolder,
            "Tasks",
            ViewType.Tasks,
            notification.UserId,
            isDefault: true // Based on user preference for 'Tasks' view as well
        );

        await db.ViewDefinitions.AddRangeAsync(new[] { overviewView, tasksView }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // STAGE 2 Notification: Background seeding is complete
        await realtime.NotifyWorkspaceAsync(notification.WorkspaceId, "FolderReady", new { FolderId = notification.FolderId, SpaceId = notification.SpaceId, WorkspaceId = notification.WorkspaceId }, cancellationToken);
    }
}
