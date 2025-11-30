using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain.Entities.Relationship;
using Domain.Events.Membership;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.EventHandlers.DomainEventHandlers.MembershipHandlers;

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
        var evt = notification;
        _logger.LogInformation("Handling WorkspaceMemberRemovedEvent for UserId: {UserId}, WorkspaceId: {WorkspaceId}",
            evt.UserId, evt.WorkspaceId);

        // Get all entities in this workspace
        var spaceIds = await _unitOfWork.Set<ProjectEntities.ProjectSpace>()
            .Where(s => s.ProjectWorkspaceId == evt.WorkspaceId)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var folderIds = await _unitOfWork.Set<ProjectEntities.ProjectFolder>()
            .Where(f => f.ProjectWorkspaceId == evt.WorkspaceId)
            .Select(f => f.Id)
            .ToListAsync(cancellationToken);

        var listIds = await _unitOfWork.Set<ProjectEntities.ProjectList>()
            .Where(l => l.ProjectWorkspaceId == evt.WorkspaceId)
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var allEntityIds = spaceIds.Concat(folderIds).Concat(listIds).ToList();

        // Remove all EntityMembers for this user in this workspace's hierarchy
        var entityMembers = await _unitOfWork.Set<EntityMember>()
            .Where(em => em.UserId == evt.UserId && allEntityIds.Contains(em.LayerId))
            .ToListAsync(cancellationToken);

        _unitOfWork.Set<EntityMember>().RemoveRange(entityMembers);

        // Invalidate permission cache
        await _permissionService.InvalidateUserCacheAsync(evt.UserId, evt.WorkspaceId);

        _logger.LogInformation("Removed {Count} EntityMembers for user {UserId} in workspace {WorkspaceId}",
            entityMembers.Count, evt.UserId, evt.WorkspaceId);
    }
}
