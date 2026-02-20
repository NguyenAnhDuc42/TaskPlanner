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

        if (space.IsPrivate)
        {
            var ownerWorkspaceMemberId = await GetWorkspaceMemberId(
                space.CreatorId ?? CurrentUserId,
                cancellationToken
            );
            await EnsureOwnerAccessAsync(space.Id, ownerWorkspaceMemberId, cancellationToken);
        }
        return Unit.Value;
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

