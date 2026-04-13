using Application.Interfaces.Data;
using Domain.Events.Workspace;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Domain.Entities;

namespace Application.EventHandlers.DomainEventHandlers.WorkspaceHandlers;

public class WorkspaceDeletedEventHandler : IDomainEventHandler<WorkspaceDeletedEvent>
{
    private readonly IDataBase _db;
    private readonly ILogger<WorkspaceDeletedEventHandler> _logger;

    public WorkspaceDeletedEventHandler(IDataBase db, ILogger<WorkspaceDeletedEventHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(WorkspaceDeletedEvent notification, CancellationToken cancellationToken)
    {
        var evt = notification;
        _logger.LogInformation("Handling WorkspaceDeletedEvent for WorkspaceId: {WorkspaceId}", evt.WorkspaceId);

        var spaceIds = await _db.Spaces
            .Where(s => s.ProjectWorkspaceId == evt.WorkspaceId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var folderIds = await _db.Folders
            .Where(f => spaceIds.Contains(f.ProjectSpaceId))
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        var allEntityIds = spaceIds.Concat(folderIds).ToList();

        // 1. Clean up EntityAccess for nested entities
        // if (allEntityIds.Any())
        // {
        //     var accessDeletedCount = await _db.Access
        //         .Where(ea => allEntityIds.Contains(ea.EntityId))
        //         .WhereNotDeleted()
        //         .ExecuteUpdateAsync(updates =>
        //             updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
        //                    .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
        //             cancellationToken: cancellationToken);
        //      _logger.LogInformation("Soft-deleted {EntityAccessCount} EntityAccess records for workspace {WorkspaceId}", accessDeletedCount, evt.WorkspaceId);
        // }

        // 2. Clean up Statuses (Linked via Workflow -> Workspace)
        var statusesDeletedCount = await _db.Statuses
            .Where(s => _db.Workflows.Any(w => w.Id == s.WorkflowId && w.ProjectWorkspaceId == evt.WorkspaceId))
            .WhereNotDeleted()
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // 3. Clean up Workflows (Workspace-level)
        var workflowsDeletedCount = await _db.Workflows
            .Where(w => w.ProjectWorkspaceId == evt.WorkspaceId)
            .WhereNotDeleted()
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // 4. Clean up Dashboards (Workspace-level)
        // var dashboardsDeletedCount = await _db.Dashboards
        //     .Where(d => d.LayerId == evt.WorkspaceId && d.LayerType == Domain.Enums.RelationShip.EntityLayerType.ProjectWorkspace)
        //     .WhereNotDeleted()
        //     .ExecuteUpdateAsync(updates =>
        //         updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
        //                .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
        //         cancellationToken: cancellationToken);

        _logger.LogInformation("Cleanup complete for workspace {WorkspaceId}: {StatusCount} Statuses, {WorkflowCount} Workflows, {DashboardCount} Dashboards",
            evt.WorkspaceId, statusesDeletedCount, workflowsDeletedCount, 0);
    }
}