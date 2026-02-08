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

namespace Application.Features.SpaceFeatures.SelfManagement.UpdateSpace;

public class UpdateSpaceHandler : BaseFeatureHandler, IRequestHandler<UpdateSpaceCommand, Unit>
{
    public UpdateSpaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await FindOrThrowAsync<ProjectSpace>(request.SpaceId);

        // Update basic properties
        if (request.Name != null || request.Description != null || request.Color != null || request.Icon != null)
        {
            space.UpdateDetails(
                request.Name ?? space.Name,
                request.Description ?? space.Description,
                request.Color ?? space.Customization.Color,
                request.Icon ?? space.Customization.Icon
            );
        }

        // Handle privacy + member management
        bool willBePrivate = request.IsPrivate ?? space.IsPrivate;

        if (willBePrivate)
        {
            var existingAccess = await UnitOfWork.Set<EntityAccess>()
                .Where(ea => ea.EntityId == space.Id && ea.EntityLayer == EntityLayerType.ProjectSpace)
                .ToListAsync(cancellationToken);

            if (!existingAccess.Any())
            {
                var memberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);
                var ownerAccess = EntityAccess.Create(
                    memberId,
                    space.Id,
                    EntityLayerType.ProjectSpace,
                    AccessLevel.Manager,
                    CurrentUserId
                );
                await UnitOfWork.Set<EntityAccess>().AddAsync(ownerAccess, cancellationToken);
                existingAccess.Add(ownerAccess);
            }

            if (request.MemberIdsToAdd?.Any() == true)
            {
                // Resolve existing user IDs to compare (this is a bit more complex now since we have memberIds)
                var existingMemberIds = await UnitOfWork.Set<WorkspaceMember>()
                    .Where(wm => existingAccess.Select(ea => ea.WorkspaceMemberId).Contains(wm.Id))
                    .Select(wm => wm.UserId)
                    .ToListAsync(cancellationToken);

                var newUserIds = request.MemberIdsToAdd
                    .Where(id => !existingMemberIds.Contains(id))
                    .ToList();

                if (newUserIds.Any())
                {
                    var newWorkspaceMemberIds = await GetWorkspaceMemberIds(newUserIds, cancellationToken);

                    var newAccessRecords = newWorkspaceMemberIds.Select(memberId =>
                        EntityAccess.Create(
                            memberId,
                            space.Id,
                            EntityLayerType.ProjectSpace, // This should probably be EntityLayerType.ProjectSpace? check enum
                            AccessLevel.Editor,
                            CurrentUserId
                        ));
                        
                    // Wait, let's check EntityLayerType enum
                    // Actually, the previous code used EntityLayerType.ProjectSpace.
                    
                    var correctAccessRecords = newWorkspaceMemberIds.Select(memberId =>
                        EntityAccess.Create(
                            memberId,
                            space.Id,
                            EntityLayerType.ProjectSpace,
                            AccessLevel.Editor,
                            CurrentUserId
                        ));
                        
                    await UnitOfWork.Set<EntityAccess>().AddRangeAsync(correctAccessRecords, cancellationToken);
                }
            }
        }

        if (request.IsPrivate.HasValue && request.IsPrivate.Value != space.IsPrivate)
        {
            space.UpdatePrivacy(request.IsPrivate.Value);
        }

        return Unit.Value;
    }
}
