using Application.Interfaces.Repositories;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using Application.Features.StatusManagement.Helpers;

namespace Application.Features.FolderFeatures.SelfManagement.UpdateFolder;

public class UpdateFolderHandler : BaseFeatureHandler, IRequestHandler<UpdateFolderCommand, Unit>
{
    public UpdateFolderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = await UnitOfWork.Set<ProjectFolder>().FindAsync(request.FolderId, cancellationToken);
        if (folder == null) throw new KeyNotFoundException($"Folder {request.FolderId} not found");
        if (request.Name is not null) folder.UpdateName(request.Name);
        if (request.Description is not null) folder.UpdateDescription(request.Description);
        if (request.Color is not null) folder.UpdateColor(request.Color);
        if (request.Icon is not null) folder.UpdateIcon(request.Icon);
        if (request.IsPrivate.HasValue) folder.UpdatePrivate(request.IsPrivate.Value);

        if (request.StartDate != null || request.DueDate != null)
        {
            folder.UpdateDates(
                request.StartDate ?? folder.StartDate,
                request.DueDate ?? folder.DueDate);
        }



        if (folder.IsPrivate)
        {
            var ownerWorkspaceMemberId = await GetWorkspaceMemberId(
                folder.CreatorId ?? CurrentUserId,
                cancellationToken
            );
            await EnsureOwnerAccessAsync(folder.Id, ownerWorkspaceMemberId, cancellationToken);
        }
        return Unit.Value;
    }

    private async Task EnsureOwnerAccessAsync(Guid folderId, Guid ownerWorkspaceMemberId, CancellationToken cancellationToken)
    {
        var ownerAccess = await UnitOfWork.Set<EntityAccess>()
            .Where(ea =>
                ea.ProjectWorkspaceId == WorkspaceId &&
                ea.EntityId == folderId &&
                ea.EntityLayer == EntityLayerType.ProjectFolder &&
                ea.WorkspaceMemberId == ownerWorkspaceMemberId &&
                ea.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerAccess is null)
        {
            var newOwnerAccess = EntityAccess.Create(
                WorkspaceId,
                ownerWorkspaceMemberId,
                folderId,
                EntityLayerType.ProjectFolder,
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
