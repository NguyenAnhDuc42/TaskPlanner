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

namespace Application.Features.FolderFeatures.SelfManagement.UpdateFolder;

public class UpdateFolderHandler : BaseFeatureHandler, IRequestHandler<UpdateFolderCommand, Unit>
{
    public UpdateFolderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = await FindOrThrowAsync<ProjectFolder>(request.FolderId);

        // Update basic properties
        if (request.Name != null || request.Color != null || request.Icon != null)
        {
            folder.UpdateDetails(
                request.Name ?? folder.Name,
                request.Color ?? folder.Customization.Color,
                request.Icon ?? folder.Customization.Icon
            );
        }

        // Handle privacy changes + member management
        bool willBePrivate = request.IsPrivate ?? folder.IsPrivate;

        if (willBePrivate)
        {
            // Check if folder already has EntityAccess
            var existingAccess = await UnitOfWork.Set<EntityAccess>()
                .Where(ea => ea.EntityId == folder.Id && ea.EntityLayer == EntityLayerType.ProjectFolder)
                .ToListAsync(cancellationToken);

            // If NO access records exist, create owner access
            if (!existingAccess.Any())
            {
                var memberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);
                var ownerAccess = EntityAccess.Create(
                    memberId,
                    folder.Id,
                    EntityLayerType.ProjectFolder,
                    AccessLevel.Manager,
                    CurrentUserId
                );
                await UnitOfWork.Set<EntityAccess>().AddAsync(ownerAccess, cancellationToken);
                existingAccess.Add(ownerAccess);
            }

            // If user is adding members
            if (request.MemberIdsToAdd?.Any() == true)
            {
                // Resolve existing user IDs to compare
                var existingMemberIds = await UnitOfWork.Set<WorkspaceMember>()
                    .Where(wm => existingAccess.Select(ea => ea.WorkspaceMemberId).Contains(wm.Id))
                    .Select(wm => wm.UserId)
                    .ToListAsync(cancellationToken);

                var newUserIds = request.MemberIdsToAdd
                    .Where(id => !existingMemberIds.Contains(id))
                    .ToList();

                if (newUserIds.Any())
                {
                    // Resolve workspace member IDs for new invitees
                    var newWorkspaceMemberIds = await GetWorkspaceMemberIds(newUserIds, cancellationToken);

                    // Create EntityAccess records for new invitees
                    var newAccessRecords = newWorkspaceMemberIds.Select(memberId =>
                        EntityAccess.Create(
                            memberId,
                            folder.Id,
                            EntityLayerType.ProjectFolder,
                            AccessLevel.Editor,
                            CurrentUserId
                        ));
                    await UnitOfWork.Set<EntityAccess>().AddRangeAsync(newAccessRecords, cancellationToken);
                }
            }
        }

        // Update privacy flag (keep EntityAccess records even if changing back to public)
        if (request.IsPrivate.HasValue && request.IsPrivate.Value != folder.IsPrivate)
        {
            folder.UpdatePrivacy(request.IsPrivate.Value);
        }

        return Unit.Value;
    }
}
