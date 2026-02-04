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

namespace Application.Features.ListFeatures.SelfManagement.UpdateList;

public class UpdateListHandler : BaseFeatureHandler, IRequestHandler<UpdateListCommand, Unit>
{
    public UpdateListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateListCommand request, CancellationToken cancellationToken)
    {
        var list = await FindOrThrowAsync<ProjectList>(request.ListId);

        // Update basic properties
        if (request.Name != null || request.Color != null || request.Icon != null)
        {
            list.UpdateDetails(
                request.Name ?? list.Name,
                request.Color ?? list.Customization.Color,
                request.Icon ?? list.Customization.Icon
            );
        }

        // Update dates
        if (request.StartDate.HasValue || request.DueDate.HasValue)
        {
            list.UpdateDates(
                request.StartDate ?? list.StartDate,
                request.DueDate ?? list.DueDate
            );
        }

        // Handle privacy + member management
        bool willBePrivate = request.IsPrivate ?? list.IsPrivate;

        if (willBePrivate)
        {
            // Check if list already has EntityAccess
            var existingAccess = await UnitOfWork.Set<EntityAccess>()
                .Where(ea => ea.EntityId == list.Id && ea.EntityLayer == EntityLayerType.ProjectList)
                .ToListAsync(cancellationToken);

            // If NO access exists, create owner access
            if (!existingAccess.Any())
            {
                var memberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);
                var ownerAccess = EntityAccess.Create(
                    memberId,
                    list.Id,
                    EntityLayerType.ProjectList,
                    AccessLevel.Manager,
                    CurrentUserId
                );
                await UnitOfWork.Set<EntityAccess>().AddAsync(ownerAccess, cancellationToken);
                existingAccess.Add(ownerAccess);
            }

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
                            list.Id,
                            EntityLayerType.ProjectList,
                            AccessLevel.Editor,
                            CurrentUserId
                        ));
                    await UnitOfWork.Set<EntityAccess>().AddRangeAsync(newAccessRecords, cancellationToken);
                }
            }
        }

        if (request.IsPrivate.HasValue && request.IsPrivate.Value != list.IsPrivate)
        {
            list.UpdatePrivacy(request.IsPrivate.Value);
        }

        return Unit.Value;
    }
}
