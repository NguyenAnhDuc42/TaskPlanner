using Application.Interfaces.Data;
using Domain.Events.Workspace;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Domain.Entities;

namespace Application.EventHandlers.DomainEventHandlers.WorkspaceHandlers;

public class WorkspaceDeletedEventHandler(
    IDataBase db, 
    ILogger<WorkspaceDeletedEventHandler> logger, 
    IRealtimeService realtime
) : IDomainEventHandler<WorkspaceDeletedEvent>
{
    public async Task Handle(WorkspaceDeletedEvent notification, CancellationToken cancellationToken)
    {
        var evt = notification;
        logger.LogInformation("Handling deep cleanup for WorkspaceId: {WorkspaceId}", evt.WorkspaceId);

        // 1. Cascading Soft-Delete for Spaces
        var spaceDeletedCount = await db.Spaces
            .Where(s => s.ProjectWorkspaceId == evt.WorkspaceId)
            .WhereNotDeleted()
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // 2. Cascading Soft-Delete for Folders
        var folderDeletedCount = await db.Folders
            .Where(f => db.Spaces.Any(s => s.Id == f.ProjectSpaceId && s.ProjectWorkspaceId == evt.WorkspaceId))
            .WhereNotDeleted()
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // 3. Cascading Soft-Delete for Tasks
        var taskDeletedCount = await db.Tasks
            .Where(t => t.ProjectWorkspaceId == evt.WorkspaceId)
            .WhereNotDeleted()
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // 4. Clean up Statuses (Linked via Workflow -> Workspace)
        var statusesDeletedCount = await db.Statuses
            .Where(s => db.Workflows.Any(w => w.Id == s.WorkflowId && w.ProjectWorkspaceId == evt.WorkspaceId))
            .WhereNotDeleted()
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // 5. Clean up Workflows (Workspace-level)
        var workflowsDeletedCount = await db.Workflows
            .Where(w => w.ProjectWorkspaceId == evt.WorkspaceId)
            .WhereNotDeleted()
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        logger.LogInformation("Cleanup complete for workspace {WorkspaceId}: {SpaceCount} Spaces, {FolderCount} Folders, {TaskCount} Tasks, {StatusCount} Statuses, {WorkflowCount} Workflows",
            evt.WorkspaceId, spaceDeletedCount, folderDeletedCount, taskDeletedCount, statusesDeletedCount, workflowsDeletedCount);

        // STAGE 2 Notification: Deep cleanup complete
        await realtime.NotifyWorkspaceAsync(evt.WorkspaceId, "WorkspacePermanentlyDeleted", new { WorkspaceId = evt.WorkspaceId }, cancellationToken);
    }
}