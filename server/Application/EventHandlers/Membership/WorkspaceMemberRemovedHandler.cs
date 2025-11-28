using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain.Entities.Relationship;
using Domain.Events.Membership;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.Membership;

public class WorkspaceMemberRemovedHandler : INotificationHandler<WorkspaceMemberRemovedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<WorkspaceMemberRemovedHandler> _logger;

    public WorkspaceMemberRemovedHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        ILogger<WorkspaceMemberRemovedHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task Handle(WorkspaceMemberRemovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling WorkspaceMemberRemovedEvent for UserId: {UserId}, WorkspaceId: {WorkspaceId}",
            notification.UserId, notification.WorkspaceId);

        // Get all entities in this workspace
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

        // Remove all EntityMembers for this user in this workspace's hierarchy
        var entityMembers = await _unitOfWork.Set<EntityMember>()
            .Where(em => em.UserId == notification.UserId && allEntityIds.Contains(em.LayerId))
            .ToListAsync(cancellationToken);

        _unitOfWork.Set<EntityMember>().RemoveRange(entityMembers);

        // Invalidate permission cache
        await _permissionService.InvalidateUserCacheAsync(notification.UserId, notification.WorkspaceId);

        _logger.LogInformation("Removed {Count} EntityMembers for user {UserId} in workspace {WorkspaceId}",
            entityMembers.Count, notification.UserId, notification.WorkspaceId);
    }
}
