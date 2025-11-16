using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.SpaceFeatures.MemberManagement.EditMembersInSpace;

public class EditMembersInSpaceHandler : BaseCommandHandler, IRequestHandler<EditMembersInSpaceCommand, Unit>
{
    public EditMembersInSpaceHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(EditMembersInSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(request.spaceId, cancellationToken) ?? throw new KeyNotFoundException("Space not found");
        await RequirePermissionAsync(space, EntityType.EntityMember, PermissionAction.Edit, cancellationToken);
        var membersToUpdate = await UnitOfWork.Set<EntityMember>()
            .Where(em => em.LayerId == space.Id
                     && em.LayerType == EntityLayerType.ProjectSpace
                     && request.membersId.Contains(em.UserId)).ToListAsync(cancellationToken);

        if (membersToUpdate.Any())
        {
            membersToUpdate.ForEach(em => em.UpdateAccessLevel(request.accessLevel));
        }
        return Unit.Value;

    }
}
