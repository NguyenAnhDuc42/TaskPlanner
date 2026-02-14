using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.ListFeatures.SelfManagement.UpdateList;

public class UpdateListHandler : BaseFeatureHandler, IRequestHandler<UpdateListCommand, Unit>
{
    public UpdateListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateListCommand request, CancellationToken cancellationToken)
    {
        var list = await FindOrThrowAsync<ProjectList>(request.ListId);
        if (request.Name is not null) list.UpdateName(request.Name);
        if (request.Color is not null) list.UpdateColor(request.Color);
        if (request.Icon is not null) list.UpdateIcon(request.Icon);
        if (request.IsPrivate.HasValue) list.UpdatePrivate(request.IsPrivate.Value);
        if (request.StartDate.HasValue) list.UpdateStartDate(request.StartDate);
        if (request.DueDate.HasValue) list.UpdateDueDate(request.DueDate);

        if (request.MembersToAddOrUpdate != null && request.MembersToAddOrUpdate.Any())
        {
            await UpdateMembersAsync(list.Id, request.MembersToAddOrUpdate, cancellationToken);
        }
        return Unit.Value;
    }

    private async Task UpdateMembersAsync(Guid listId, List<UpdateListMemberValue> members, CancellationToken cancellationToken)
    {
        var existingMembers = await UnitOfWork.Set<EntityAccess>()
            .Where(ea => ea.EntityId == listId && ea.EntityLayer == EntityLayerType.ProjectList)
            .ToListAsync(cancellationToken);

        var existingMap = existingMembers.ToDictionary(em => em.WorkspaceMemberId);
        var updateMap = members.ToDictionary(m => m.workspaceMemberId);

        var toAdd = updateMap.Keys.Except(existingMap.Keys).ToList();
        var toUpdate = updateMap.Keys.Intersect(existingMap.Keys).ToList();
        var toRemove = existingMap.Keys.Except(updateMap.Keys).ToList();

        if (toAdd.Any())
        {
            var newRecords = toAdd.Select(memberId =>
                EntityAccess.Create(memberId, listId, EntityLayerType.ProjectList,
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
