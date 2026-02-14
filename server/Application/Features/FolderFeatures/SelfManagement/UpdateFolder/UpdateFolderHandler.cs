using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.FolderFeatures.SelfManagement.UpdateFolder;

public class UpdateFolderHandler : BaseFeatureHandler, IRequestHandler<UpdateFolderCommand, Unit>
{
    public UpdateFolderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = await FindOrThrowAsync<ProjectFolder>(request.FolderId);
        if (request.Name is not null) folder.UpdateName(request.Name);
        if (request.Color is not null) folder.UpdateColor(request.Color);
        if (request.Icon is not null) folder.UpdateIcon(request.Icon);
        if (request.IsPrivate.HasValue) folder.UpdatePrivate(request.IsPrivate.Value);

        if (request.MembersToAddOrUpdate != null && request.MembersToAddOrUpdate.Any())
        {
            await UpdateMembersAsync(folder.Id, request.MembersToAddOrUpdate, cancellationToken);
        }
        return Unit.Value;
    }

    private async Task UpdateMembersAsync(Guid folderId, List<UpdateFolderMemberValue> members, CancellationToken cancellationToken)
    {
        var existingMembers = await UnitOfWork.Set<EntityAccess>()
            .Where(ea => ea.EntityId == folderId && ea.EntityLayer == EntityLayerType.ProjectFolder)
            .ToListAsync(cancellationToken);

        var existingMap = existingMembers.ToDictionary(em => em.WorkspaceMemberId);
        var updateMap = members.ToDictionary(m => m.workspaceMemberId);

        var toAdd = updateMap.Keys.Except(existingMap.Keys).ToList();
        var toUpdate = updateMap.Keys.Intersect(existingMap.Keys).ToList();
        var toRemove = existingMap.Keys.Except(updateMap.Keys).ToList();

        if (toAdd.Any())
        {
            var newRecords = toAdd.Select(memberId =>
                EntityAccess.Create(memberId, folderId, EntityLayerType.ProjectFolder,
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
