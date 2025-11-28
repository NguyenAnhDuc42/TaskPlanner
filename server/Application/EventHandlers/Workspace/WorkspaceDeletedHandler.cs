using Application.Interfaces.Repositories;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Events.Workspace;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.Workspace;

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
        _logger.LogInformation("Handling WorkspaceDeletedEvent for WorkspaceId: {WorkspaceId}", notification.WorkspaceId);

        // Cleanup EntityMembers for all entities in this workspace's hierarchy
        // Since Space/Folder/List belong to workspace, we need to find all their EntityMembers
        var spaceIds = await _unitOfWork.Set<ProjectEntities.ProjectSpace>()
            .Where(s => s.ProjectWorkspaceId == notification.WorkspaceId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var folderIds = await _unitOfWork.Set<ProjectEntities.ProjectFolder>()
            .Where(f => f.ProjectWorkspaceId == notification.WorkspaceId)
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        var listIds = await _unitOfWork.Set<ProjectEntities.ProjectList>()
            .Where(l => l.ProjectWorkspaceId == notification.WorkspaceId)
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
            entityMembers.Count, statuses.Count, notification.WorkspaceId);
    }
}
