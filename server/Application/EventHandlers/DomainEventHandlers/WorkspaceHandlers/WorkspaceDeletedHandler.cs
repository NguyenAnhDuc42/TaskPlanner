using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Events.Workspace;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.WorkspaceHandlers;

public class WorkspaceDeletedHandler : INotificationHandler<WorkspaceDeletedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WorkspaceDeletedHandler> _logger;

    public WorkspaceDeletedHandler(IUnitOfWork unitOfWork, ILogger<WorkspaceDeletedHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(WorkspaceDeletedEvent notification, CancellationToken cancellationToken)
    {
        var evt = notification;
        _logger.LogInformation("Handling WorkspaceDeletedEvent for WorkspaceId: {WorkspaceId}", evt.WorkspaceId);

        // Cleanup EntityMembers for all entities in this workspace's hierarchy
        var spaceIds = await _unitOfWork.Set<ProjectSpace>()
            .Where(s => s.ProjectWorkspaceId == evt.WorkspaceId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var folderIds = await _unitOfWork.Set<ProjectFolder>()
            .Where(f => f.ProjectWorkspaceId == evt.WorkspaceId)
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        var listIds = await _unitOfWork.Set<ProjectList>()
            .Where(l => l.ProjectWorkspaceId == evt.WorkspaceId)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var allEntityIds = spaceIds.Concat(folderIds).Concat(listIds).ToList();

        // Remove all EntityMembers for these entities
        var entityMembers = await _unitOfWork.Set<EntityMember>()
            .Where(em => allEntityIds.Contains(em.LayerId))
            .ToListAsync(cancellationToken);

        _unitOfWork.Set<EntityMember>().RemoveRange(entityMembers);

        // Cleanup Status entities for this workspace's layers
        var statuses = await _unitOfWork.Set<Status>()
            .Where(s => allEntityIds.Contains(s.LayerId!.Value))
            .ToListAsync(cancellationToken);

        _unitOfWork.Set<Status>().RemoveRange(statuses);

        _logger.LogInformation("Cleaned up {EntityMemberCount} EntityMembers and {StatusCount} Statuses for workspace {WorkspaceId}",
            entityMembers.Count, statuses.Count, evt.WorkspaceId);
    }
}
