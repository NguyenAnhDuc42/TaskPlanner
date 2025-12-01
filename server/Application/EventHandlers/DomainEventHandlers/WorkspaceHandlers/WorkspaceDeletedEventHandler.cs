using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
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

        if (!allEntityIds.Any())
        {
            _logger.LogInformation("No nested entities found to clean up for workspace {WorkspaceId}", evt.WorkspaceId);
            return;
        }

        var membersDeletedCount = await _unitOfWork.Set<EntityMember>()
            .Where(em => allEntityIds.Contains(em.LayerId) && em.DeletedAt == null)
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow), // Also update the UpdatedAt timestamp
                cancellationToken: cancellationToken);

        var statusesDeletedCount = await _unitOfWork.Set<Status>()
            .Where(s => s.LayerId.HasValue && allEntityIds.Contains(s.LayerId.Value) && s.DeletedAt == null)
            .ExecuteUpdateAsync(updates =>
                updates.SetProperty(e => e.DeletedAt, DateTimeOffset.UtcNow)
                       .SetProperty(e => e.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);

        _logger.LogInformation("Soft-deleted {EntityMemberCount} EntityMembers and {StatusCount} Statuses for workspace {WorkspaceId}",
            membersDeletedCount, statusesDeletedCount, evt.WorkspaceId);
    }
}