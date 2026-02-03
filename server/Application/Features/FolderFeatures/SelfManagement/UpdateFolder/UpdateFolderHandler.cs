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
            // Check if folder already has EntityMembers
            var existingMembers = await UnitOfWork.Set<EntityMember>()
                .Where(em => em.LayerId == folder.Id && em.LayerType == EntityLayerType.ProjectFolder)
                .ToListAsync(cancellationToken);

            // If NO members exist, create owner member
            if (!existingMembers.Any())
            {
                var ownerMember = EntityMember.AddMember(
                    CurrentUserId,
                    folder.Id,
                    EntityLayerType.ProjectFolder,
                    AccessLevel.Manager,
                    CurrentUserId
                );
                await UnitOfWork.Set<EntityMember>().AddAsync(ownerMember, cancellationToken);
                existingMembers.Add(ownerMember);
            }

            // If user is adding members
            if (request.MemberIdsToAdd?.Any() == true)
            {
                var existingMemberIds = existingMembers.Select(m => m.UserId).ToHashSet();
                var newMemberIds = request.MemberIdsToAdd
                    .Where(id => !existingMemberIds.Contains(id))
                    .ToList();

                if (newMemberIds.Any())
                {
                    // Validate workspace membership
                    var validMembers = await ValidateWorkspaceMembers(newMemberIds, cancellationToken);

                    // Create EntityMembers for new invitees
                    var newMembers = validMembers.Select(userId =>
                        EntityMember.AddMember(
                            userId,
                            folder.Id,
                            EntityLayerType.ProjectFolder,
                            AccessLevel.Editor,
                            CurrentUserId
                        ));
                    await UnitOfWork.Set<EntityMember>().AddRangeAsync(newMembers, cancellationToken);
                }
            }
        }

        // Update privacy flag (keep EntityMembers even if changing back to public)
        if (request.IsPrivate.HasValue && request.IsPrivate.Value != folder.IsPrivate)
        {
            folder.UpdatePrivacy(request.IsPrivate.Value);
        }

        return Unit.Value;
    }
}
