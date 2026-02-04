using System;
using Application.Interfaces.Repositories;
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

public class EditSpaceHandler : BaseFeatureHandler, IRequestHandler<EditSpaceCommand, Unit>
{
    public EditSpaceHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext
    ) : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(EditSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await UnitOfWork.Set<ProjectSpace>()
            .FindAsync(request.spaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Space not found");

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
        // Fetch only relevant access records to minimize DB load
        var accessRecords = await UnitOfWork.Set<EntityAccess>()
            .Where(ea => ea.EntityId == spaceId
                      && ea.EntityLayer == EntityLayerType.ProjectSpace)
            .ToListAsync(cancellationToken);
            
        // We need to check which workspace members these access records belong to
        var workspaceMembers = await UnitOfWork.Set<WorkspaceMember>()
            .Where(wm => (wm.UserId == creatorId || wm.UserId == currentUserId) && wm.ProjectWorkspaceId == WorkspaceId)
            .ToListAsync(cancellationToken);

        var creatorMember = workspaceMembers.FirstOrDefault(wm => wm.UserId == creatorId);
        var currentUserMember = workspaceMembers.FirstOrDefault(wm => wm.UserId == currentUserId);

        bool creatorHasAccess = creatorMember != null && accessRecords.Any(ea => ea.WorkspaceMemberId == creatorMember.Id);
        bool currentUserHasAccess = currentUserMember != null && accessRecords.Any(ea => ea.WorkspaceMemberId == currentUserMember.Id);

        var toAdd = new List<EntityAccess>();

        if (!creatorHasAccess && creatorMember != null)
            toAdd.Add(EntityAccess.Create(creatorMember.Id, spaceId, EntityLayerType.ProjectSpace, AccessLevel.Manager, creatorId));

        if (!currentUserHasAccess && currentUserMember != null && currentUserId != creatorId)
            toAdd.Add(EntityAccess.Create(currentUserMember.Id, spaceId, EntityLayerType.ProjectSpace, AccessLevel.Editor, currentUserId));

        if (toAdd.Any())
        {
            await UnitOfWork.Set<EntityAccess>().AddRangeAsync(toAdd, cancellationToken);
        }
    }
}
