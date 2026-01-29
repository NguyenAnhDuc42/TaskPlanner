using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.SpaceFeatures.SelfManagement.EditSpace;

public class EditSpaceHandler : BaseCommandHandler, IRequestHandler<EditSpaceCommand, Unit>
{
    public EditSpaceHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext
    ) : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(EditSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await UnitOfWork.Set<ProjectSpace>()
            .FindAsync(request.spaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Space not found");

        await RequirePermissionAsync(space, PermissionAction.Edit, cancellationToken);

        // Update space properties
        space.Update(
            name: request.name,
            description: request.description,
            color: request.color,
            icon: request.icon,
            isPrivate: request.isPrivate,
            isArchived: request.isArchived
        );

        UnitOfWork.Set<ProjectSpace>().Update(space);

        // Ensure members if private
        if (request.isPrivate && space.CreatorId.HasValue)
        {
            await EnsureSpaceHasMembersAsync(space.Id, space.CreatorId.Value, CurrentUserId, cancellationToken);
        }

        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private async Task EnsureSpaceHasMembersAsync(Guid spaceId, Guid creatorId, Guid currentUserId, CancellationToken cancellationToken)
    {
        // Fetch only relevant members to minimize DB load
        var members = await UnitOfWork.Set<EntityMember>()
            .Where(em => em.LayerId == spaceId
                      && em.LayerType == EntityLayerType.ProjectSpace
                      && (em.UserId == creatorId || em.UserId == currentUserId))
            .ToListAsync(cancellationToken);

        bool creatorExists = members.Any(em => em.UserId == creatorId);
        bool currentUserExists = members.Any(em => em.UserId == currentUserId);

        var toAdd = new List<EntityMember>();

        if (!creatorExists)
            toAdd.Add(EntityMember.AddMember(creatorId, spaceId, EntityLayerType.ProjectSpace, AccessLevel.Manager, creatorId));

        if (!currentUserExists && currentUserId != creatorId)
            toAdd.Add(EntityMember.AddMember(currentUserId, spaceId, EntityLayerType.ProjectSpace, AccessLevel.Editor, currentUserId));

        if (toAdd.Any())
        {
            await UnitOfWork.Set<EntityMember>().AddRangeAsync(toAdd, cancellationToken);
        }
    }
}
