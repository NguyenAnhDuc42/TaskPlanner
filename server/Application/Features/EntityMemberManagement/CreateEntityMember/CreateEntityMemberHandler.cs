using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Application.Features.EntityMemberManagement.CreateEntityMember;

public class CreateEntityMemberHandler : BaseCommandHandler, IRequestHandler<CreateEntityMemberCommand, Unit>
{
    public CreateEntityMemberHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(CreateEntityMemberCommand request, CancellationToken cancellationToken)
    {
        // Get parent layer (Space/Folder/List) using GetLayer helper
        var layer = await GetLayer(request.LayerId, request.LayerType);

        // Permission check
        await RequirePermissionAsync(layer, EntityType.EntityMember, PermissionAction.Create, cancellationToken);

        // Validate all users are workspace members
        var workspaceMembers = await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => request.UserIds.Contains(wm.UserId) && wm.ProjectWorkspaceId == WorkspaceId)
            .ToListAsync(cancellationToken);

        if (workspaceMembers.Count != request.UserIds.Count)
            throw new ValidationException("One or more users are not members of this workspace");

        // Check for existing members
        var existingMemberIds = await UnitOfWork.Set<EntityMember>()
            .Where(em => em.LayerId == request.LayerId 
                      && em.LayerType == request.LayerType 
                      && request.UserIds.Contains(em.UserId))
            .Select(em => em.UserId)
            .ToListAsync(cancellationToken);

        // Filter out users who are already members
        var newUserIds = request.UserIds.Except(existingMemberIds).ToList();

        if (!newUserIds.Any())
            return Unit.Value;

        // Create new members
        var newMembers = newUserIds
            .Select(userId => EntityMember.AddMember(userId, request.LayerId, request.LayerType, request.AccessLevel, CurrentUserId))
            .ToList();

        await UnitOfWork.Set<EntityMember>().AddRangeAsync(newMembers, cancellationToken);

        return Unit.Value;
    }
}
