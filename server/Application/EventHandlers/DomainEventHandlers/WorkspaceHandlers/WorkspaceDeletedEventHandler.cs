using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Events.Workspace;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.WorkspaceHandlers;

public class WorkspaceDeletedEventHandler : INotificationHandler<WorkspaceDeletedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WorkspaceDeletedEventHandler> _logger;

    public WorkspaceDeletedEventHandler(IUnitOfWork unitOfWork, ILogger<WorkspaceDeletedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(WorkspaceDeletedEvent notification, CancellationToken cancellationToken)
    {
        var evt = notification;
        _logger.LogInformation("Handling WorkspaceDeletedEvent for WorkspaceId: {WorkspaceId}", evt.WorkspaceId);


        var spaceIds = await _unitOfWork.Set<ProjectSpace>()
            .Where(s => s.ProjectWorkspaceId == evt.WorkspaceId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var folderIds = await _unitOfWork.Set<ProjectFolder>()
            .Where(f => spaceIds.Contains(f.ProjectSpaceId))
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        var listIds = await _unitOfWork.Set<ProjectList>()
            .Where(l => l.ProjectFolderId != null && folderIds.Contains(l.ProjectFolderId.Value) && spaceIds.Contains(l.ProjectSpaceId))
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var allEntityIds = spaceIds.Concat(folderIds).Concat(listIds).ToList();

        // 1. Clean up EntityAccess for nested entities
        if (allEntityIds.Any())
        {
            var accessDeletedCount = await _unitOfWork.Set<EntityAccess>()
                .Where(ea => allEntityIds.Contains(ea.EntityId) && ea.DeletedAt == null)
                .ExecuteUpdateAsync(updates =>
                    updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                           .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                    cancellationToken: cancellationToken);
             _logger.LogInformation("Soft-deleted {EntityAccessCount} EntityAccess records for workspace {WorkspaceId}", accessDeletedCount, evt.WorkspaceId);
        }

        // 2. Clean up Statuses (both Workspace-level and Entity-level)
        // Combine WorkspaceId with allEntityIds for status cleanup
        var allLayersForStatus = new List<Guid> { evt.WorkspaceId };
        allLayersForStatus.AddRange(allEntityIds);

        var statusesDeletedCount = await _unitOfWork.Set<Status>()
            .Where(s => s.LayerId.HasValue && allLayersForStatus.Contains(s.LayerId.Value) && s.DeletedAt == null)
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        // 3. Clean up Dashboards (Workspace-level)
        var dashboardsDeletedCount = await _unitOfWork.Set<Dashboard>()
            .Where(d => d.LayerId == evt.WorkspaceId && d.LayerType == Domain.Enums.RelationShip.EntityLayerType.ProjectWorkspace && d.DeletedAt == null)
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        _logger.LogInformation("Cleanup complete for workspace {WorkspaceId}: {StatusCount} Statuses, {DashboardCount} Dashboards",
            evt.WorkspaceId, statusesDeletedCount, dashboardsDeletedCount);

    }
}