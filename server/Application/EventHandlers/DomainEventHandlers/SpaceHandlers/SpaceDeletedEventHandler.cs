using Application.Interfaces.Data;
using Domain.Events.Space;
using Application.Common.Interfaces;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace Application.EventHandlers.DomainEventHandlers.SpaceHandlers;

public class SpaceDeletedEventHandler(
    IDataBase db, 
    ILogger<SpaceDeletedEventHandler> logger, 
    IRealtimeService realtime
) : IDomainEventHandler<SpaceDeletedEvent>
{
    public async Task Handle(SpaceDeletedEvent notification, CancellationToken cancellationToken)
    {
        var evt = notification;
        logger.LogInformation("Handling deep cleanup for SpaceId: {SpaceId}", evt.SpaceId);

        // 1. Cascading Soft-Delete for Folders
        var folderDeletedCount = await db.Folders
            .Where(f => f.ProjectSpaceId == evt.SpaceId)
            .WhereNotDeleted()
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // 2. Cascading Soft-Delete for Tasks
        var taskDeletedCount = await db.Tasks
            .Where(t => t.ProjectSpaceId == evt.SpaceId)
            .WhereNotDeleted()
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // 3. Cascading Soft-Delete for Views (ViewDefinitions)
        var viewsDeletedCount = await db.ViewDefinitions
            .Where(v => v.LayerId == evt.SpaceId && v.LayerType == Domain.Enums.RelationShip.EntityLayerType.ProjectSpace)
            .WhereNotDeleted()
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        logger.LogInformation("Cleanup complete for space {SpaceId}: {FolderCount} Folders, {TaskCount} Tasks, {ViewCount} Views",
            evt.SpaceId, folderDeletedCount, taskDeletedCount, viewsDeletedCount);

        // STAGE 2 Notification: Deep cleanup complete
        await realtime.NotifyWorkspaceAsync(evt.WorkspaceId, "SpacePermanentlyDeleted", new { SpaceId = evt.SpaceId, WorkspaceId = evt.WorkspaceId }, cancellationToken);
    }
}
