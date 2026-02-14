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

        if (request.MembersToAddOrUpdate != null && request.MembersToAddOrUpdate.Any())
        {
            await UpdateMembersAsync(space.Id, request.MembersToAddOrUpdate, cancellationToken);
        }
        return Unit.Value;
    }

    private async Task UpdateMembersAsync(Guid spaceId, List<UpdateSpaceMemberValue> members, CancellationToken cancellationToken)
    {
        var existingMembers = await UnitOfWork.Set<EntityAccess>()
            .Where(ea => ea.EntityId == spaceId && ea.EntityLayer == EntityLayerType.ProjectSpace)
            .ToListAsync(cancellationToken);

        // Convert to Dictionary for O(1) lookup
        var existingMap = existingMembers.ToDictionary(em => em.WorkspaceMemberId);
        var updateMap = members.ToDictionary(m => m.workspaceMemberId);

        // Determine changes
        var toAdd = updateMap.Keys.Except(existingMap.Keys).ToList();
        var toUpdate = updateMap.Keys.Intersect(existingMap.Keys).ToList();
        var toRemove = existingMap.Keys.Except(updateMap.Keys).ToList();

        if (toAdd.Any())
        {
            var newRecords = toAdd.Select(memberId =>
                EntityAccess.Create(memberId, spaceId, EntityLayerType.ProjectSpace,
                    updateMap[memberId].accessLevel ?? AccessLevel.Viewer, CurrentUserId));
            await UnitOfWork.Set<EntityAccess>().AddRangeAsync(newRecords, cancellationToken);
        }

        foreach (var memberId in toUpdate)
        {
            var access = existingMap[memberId];
            if (updateMap[memberId].accessLevel.HasValue) access.UpdateAccessLevel(updateMap[memberId].accessLevel!.Value);
        }

        foreach (var memberId in toRemove)
        {
            existingMap[memberId].Remove();
        }
    }
}

