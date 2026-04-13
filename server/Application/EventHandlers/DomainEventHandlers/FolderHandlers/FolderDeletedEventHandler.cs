using Application.Interfaces.Data;
using Domain.Events.Folder;
using Application.Common.Interfaces;
using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Domain.Enums.RelationShip;

namespace Application.EventHandlers.DomainEventHandlers.FolderHandlers;

public class FolderDeletedEventHandler(
    IDataBase db, 
    ILogger<FolderDeletedEventHandler> logger, 
    IRealtimeService realtime
) : IDomainEventHandler<FolderDeletedEvent>
{
    public async Task Handle(FolderDeletedEvent notification, CancellationToken cancellationToken)
    {
        var evt = notification;
        logger.LogInformation("Handling deep cleanup for FolderId: {FolderId}", evt.FolderId);

        // 1. Cascading Soft-Delete for Tasks
        var taskDeletedCount = await db.Tasks
            .Where(t => t.ProjectFolderId == evt.FolderId)
            .WhereNotDeleted()
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // 2. Cascading Soft-Delete for Views (ViewDefinitions)
        var viewsDeletedCount = await db.ViewDefinitions
            .Where(v => v.LayerId == evt.FolderId && v.LayerType == EntityLayerType.ProjectFolder)
            .WhereNotDeleted()
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        logger.LogInformation("Cleanup complete for folder {FolderId}: {TaskCount} Tasks, {ViewCount} Views",
            evt.FolderId, taskDeletedCount, viewsDeletedCount);

        // STAGE 2 Notification: Deep cleanup complete
        await realtime.NotifyWorkspaceAsync(evt.WorkspaceId, "FolderPermanentlyDeleted", new { FolderId = evt.FolderId, SpaceId = evt.SpaceId, WorkspaceId = evt.WorkspaceId }, cancellationToken);
    }
}
