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
        var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(request.SpaceId, cancellationToken);
        if (space == null) throw new KeyNotFoundException($"Space {request.SpaceId} not found");

        if (request.Name is not null || request.Description is not null)
        {
            var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;
            space.UpdateBasicInfo(request.Name, slug, request.Description);
        }

        if (request.Color is not null || request.Icon is not null) 
            space.UpdateCustomization(request.Color, request.Icon);

        if (request.IsPrivate.HasValue) 
            space.UpdatePrivate(request.IsPrivate.Value);

        if (space.IsPrivate)
        {
            var ownerWorkspaceMemberId = await GetWorkspaceMemberId(
                space.CreatorId ?? CurrentUserId,
                cancellationToken
            );
            await EnsureOwnerAccessAsync(space.Id, ownerWorkspaceMemberId, cancellationToken);
        }

        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private async Task EnsureOwnerAccessAsync(Guid spaceId, Guid ownerWorkspaceMemberId, CancellationToken cancellationToken)
    {
        var ownerAccess = await UnitOfWork.Set<EntityAccess>()
            .Where(ea =>
                ea.ProjectWorkspaceId == WorkspaceId &&
                ea.EntityId == spaceId &&
                ea.EntityLayer == EntityLayerType.ProjectSpace &&
                ea.WorkspaceMemberId == ownerWorkspaceMemberId &&
                ea.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerAccess is null)
        {
            var newOwnerAccess = EntityAccess.Create(
                WorkspaceId,
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

