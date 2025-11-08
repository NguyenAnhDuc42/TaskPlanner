using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.SpaceFeatures.MemberManagement.RemoveMembersFromSpace;

public class RemoveMembersFromSpaceHandler : BaseCommandHandler, IRequestHandler<RemoveMembersFromSpaceCommand, Unit>
{
    public RemoveMembersFromSpaceHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(RemoveMembersFromSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(request.spaceId, cancellationToken) ?? throw new KeyNotFoundException("Space not found");
        await RequirePermissionAsync(space, EntityType.EntityMember, PermissionAction.Delete, cancellationToken);
        var membersToRemove = await UnitOfWork.Set<EntityMember>()
            .Where(em => em.EntityId == space.Id
                     && em.EntityType == EntityLayerType.ProjectSpace
                     && request.membersId.Contains(em.UserId))
            .ToListAsync(cancellationToken);

        if (membersToRemove.Any())
        {
            UnitOfWork.Set<EntityMember>().RemoveRange(membersToRemove);
        }
        return Unit.Value;
    }
}
