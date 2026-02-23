using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using Application.Features.StatusManagement.Helpers;

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

        if (request.InheritStatus.HasValue)
        {
            var oldInherit = list.InheritStatus;
            list.UpdateInheritStatus(request.InheritStatus.Value);

            // If we just TURNED OFF inheritance, ensure we have default statuses
            if (oldInherit && !list.InheritStatus)
            {
                var hasLocalStatuses = await UnitOfWork.Set<Status>()
                    .AnyAsync(s => s.LayerId == list.Id && s.LayerType == EntityLayerType.ProjectList, cancellationToken);

                if (!hasLocalStatuses)
                {
                    await StatusInitializer.InitDefaultStatuses(UnitOfWork, list.Id, EntityLayerType.ProjectList, CurrentUserId);
                }
            }
        }

        if (list.IsPrivate)
        {
            var ownerWorkspaceMemberId = await GetWorkspaceMemberId(
                list.CreatorId ?? CurrentUserId,
                cancellationToken
            );
            await EnsureOwnerAccessAsync(list.Id, ownerWorkspaceMemberId, cancellationToken);
        }
        return Unit.Value;
    }

    private async Task EnsureOwnerAccessAsync(Guid listId, Guid ownerWorkspaceMemberId, CancellationToken cancellationToken)
    {
        var ownerAccess = await UnitOfWork.Set<EntityAccess>()
            .Where(ea =>
                ea.EntityId == listId &&
                ea.EntityLayer == EntityLayerType.ProjectList &&
                ea.WorkspaceMemberId == ownerWorkspaceMemberId &&
                ea.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerAccess is null)
        {
            var newOwnerAccess = EntityAccess.Create(
                ownerWorkspaceMemberId,
                listId,
                EntityLayerType.ProjectList,
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
