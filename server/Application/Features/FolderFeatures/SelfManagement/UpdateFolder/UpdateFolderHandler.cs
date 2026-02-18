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

        var ownerWorkspaceMemberId = await GetWorkspaceMemberId(
            folder.CreatorId ?? CurrentUserId,
            cancellationToken
        );

        if (request.MembersToAddOrUpdate != null && request.MembersToAddOrUpdate.Any())
        {
            await UpdateMembersAsync(
                folder.Id,
                ownerWorkspaceMemberId,
                request.MembersToAddOrUpdate,
                cancellationToken
            );
        }

        if (folder.IsPrivate)
        {
            await EnsureOwnerAccessAsync(folder.Id, ownerWorkspaceMemberId, cancellationToken);
        }
        return Unit.Value;
    }

    private async Task UpdateMembersAsync(
        Guid folderId,
        Guid ownerWorkspaceMemberId,
        List<UpdateFolderMemberValue> members,
        CancellationToken cancellationToken
    )
    {
        var existingMembers = await UnitOfWork.Set<EntityAccess>()
            .Where(ea =>
                ea.EntityId == folderId &&
                ea.EntityLayer == EntityLayerType.ProjectFolder &&
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
                folderId,
                EntityLayerType.ProjectFolder,
                member.accessLevel ?? AccessLevel.Viewer,
                CurrentUserId);

            await UnitOfWork.Set<EntityAccess>().AddAsync(newAccess, cancellationToken);
        }
    }

    private async Task EnsureOwnerAccessAsync(Guid folderId, Guid ownerWorkspaceMemberId, CancellationToken cancellationToken)
    {
        var ownerAccess = await UnitOfWork.Set<EntityAccess>()
            .Where(ea =>
                ea.EntityId == folderId &&
                ea.EntityLayer == EntityLayerType.ProjectFolder &&
                ea.WorkspaceMemberId == ownerWorkspaceMemberId &&
                ea.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (ownerAccess is null)
        {
            var newOwnerAccess = EntityAccess.Create(
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
