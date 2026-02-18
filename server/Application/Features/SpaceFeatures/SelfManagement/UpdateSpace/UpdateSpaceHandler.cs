using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.SpaceFeatures.SelfManagement.UpdateSpace;

public class UpdateSpaceHandler : BaseFeatureHandler, IRequestHandler<UpdateSpaceCommand, Unit>
{
    public UpdateSpaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await FindOrThrowAsync<ProjectSpace>(request.SpaceId);
        if (request.Name is not null) space.UpdateName(request.Name);
        if (request.Description is not null) space.UpdateDescription(request.Description);
        if (request.Color is not null) space.UpdateColor(request.Color);
        if (request.Icon is not null) space.UpdateIcon(request.Icon);
        if (request.IsPrivate.HasValue) space.UpdatePrivate(request.IsPrivate.Value);

        var ownerWorkspaceMemberId = await GetWorkspaceMemberId(
            space.CreatorId ?? CurrentUserId,
            cancellationToken
        );

        if (request.MembersToAddOrUpdate != null && request.MembersToAddOrUpdate.Any())
        {
            await UpdateMembersAsync(
                space.Id,
                ownerWorkspaceMemberId,
                request.MembersToAddOrUpdate,
                cancellationToken
            );
        }

        if (space.IsPrivate)
        {
            await EnsureOwnerAccessAsync(space.Id, ownerWorkspaceMemberId, cancellationToken);
        }
        return Unit.Value;
    }

    private async Task UpdateMembersAsync(
        Guid spaceId,
        Guid ownerWorkspaceMemberId,
        List<UpdateSpaceMemberValue> members,
        CancellationToken cancellationToken
    )
    {
        var existingMembers = await UnitOfWork.Set<EntityAccess>()
            .Where(ea =>
                ea.EntityId == spaceId &&
                ea.EntityLayer == EntityLayerType.ProjectSpace &&
                ea.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var existingMap = existingMembers.ToDictionary(em => em.WorkspaceMemberId);
        foreach (var member in members)
        {
            if (member.workspaceMemberId == ownerWorkspaceMemberId)
            {
                if (existingMap.TryGetValue(ownerWorkspaceMemberId, out var ownerAccess))
                {
                    ownerAccess.UpdateAccessLevel(AccessLevel.Manager);
                }
                continue;
            }

            if (existingMap.TryGetValue(member.workspaceMemberId, out var current))
            {
                if (member.isRemove)
                {
                    current.Remove();
                    continue;
                }

                if (member.accessLevel.HasValue)
                {
                    current.UpdateAccessLevel(member.accessLevel.Value);
                }

                continue;
            }

            if (member.isRemove)
            {
                continue;
            }

            var newAccess = EntityAccess.Create(
                member.workspaceMemberId,
                spaceId,
                EntityLayerType.ProjectSpace,
                member.accessLevel ?? AccessLevel.Viewer,
                CurrentUserId);

            await UnitOfWork.Set<EntityAccess>().AddAsync(newAccess, cancellationToken);
        }
    }

    private async Task EnsureOwnerAccessAsync(Guid spaceId, Guid ownerWorkspaceMemberId, CancellationToken cancellationToken)
    {
        var ownerAccess = await UnitOfWork.Set<EntityAccess>()
            .Where(ea =>
                ea.EntityId == spaceId &&
                ea.EntityLayer == EntityLayerType.ProjectSpace &&
                ea.WorkspaceMemberId == ownerWorkspaceMemberId &&
                ea.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerAccess is null)
        {
            var newOwnerAccess = EntityAccess.Create(
                ownerWorkspaceMemberId,
                spaceId,
                EntityLayerType.ProjectSpace,
                AccessLevel.Manager,
                CurrentUserId
            );

            await UnitOfWork.Set<EntityAccess>().AddAsync(newOwnerAccess, cancellationToken);
            return;
        }

        if (ownerAccess.AccessLevel != AccessLevel.Manager)
        {
            ownerAccess.UpdateAccessLevel(AccessLevel.Manager);
        }
    }
}

