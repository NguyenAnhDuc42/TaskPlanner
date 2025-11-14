using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using Domain.Enums.RelationShip;

namespace Application.Features.SpaceFeatures.MemberManagement.AddMembersToSpace;

public class AddMembersToSpaceHandler : BaseCommandHandler, IRequestHandler<AddMembersToSpaceCommand, Unit>
{
    public AddMembersToSpaceHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(AddMembersToSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(request.spaceId, cancellationToken) ?? throw new KeyNotFoundException("Space not found");
        await RequirePermissionAsync(space, EntityType.EntityMember, PermissionAction.Create, cancellationToken);
        var workspaceMembers = await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => request.membersId.Contains(wm.UserId) && wm.ProjectWorkspaceId == WorkspaceId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!workspaceMembers.Any())
            return Unit.Value;

        var existingMemberIds = await UnitOfWork.Set<EntityMember>()
            .Where(em => em.LayerId == space.Id
                     && em.LayerType == EntityLayerType.ProjectSpace
                     && request.membersId.Contains(em.UserId))
            .Select(em => em.UserId)
            .ToListAsync(cancellationToken);

        var membersToAdd = workspaceMembers
            .Where(wm => !existingMemberIds.Contains(wm.UserId))
            .Select(wm => EntityMember.AddMember(
                wm.UserId,
                space.Id,
                EntityLayerType.ProjectSpace,
                request.accessLevel,
                CurrentUserId
            ))
            .ToList();

        if (membersToAdd.Any())
        {
            await UnitOfWork.Set<EntityMember>().AddRangeAsync(membersToAdd, cancellationToken);
            await UnitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
