using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
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

public class UpdateListHandler : BaseCommandHandler, IRequestHandler<UpdateListCommand, Unit>
{
    public UpdateListHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateListCommand request, CancellationToken cancellationToken)
    {
        var list = await FindOrThrowAsync<ProjectList>(request.ListId);

        await RequirePermissionAsync(list, PermissionAction.Edit, cancellationToken);

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
            var existingMembers = await UnitOfWork.Set<EntityMember>()
                .Where(em => em.LayerId == list.Id && em.LayerType == EntityLayerType.ProjectList)
                .ToListAsync(cancellationToken);

            if (!existingMembers.Any())
            {
                var ownerMember = EntityMember.AddMember(
                    CurrentUserId,
                    list.Id,
                    EntityLayerType.ProjectList,
                    AccessLevel.Manager,
                    CurrentUserId
                );
                await UnitOfWork.Set<EntityMember>().AddAsync(ownerMember, cancellationToken);
                existingMembers.Add(ownerMember);
            }

            if (request.MemberIdsToAdd?.Any() == true)
            {
                var existingMemberIds = existingMembers.Select(m => m.UserId).ToHashSet();
                var newMemberIds = request.MemberIdsToAdd
                    .Where(id => !existingMemberIds.Contains(id))
                    .ToList();

                if (newMemberIds.Any())
                {
                    var validMembers = await ValidateWorkspaceMembers(newMemberIds, cancellationToken);

                    var newMembers = validMembers.Select(userId =>
                        EntityMember.AddMember(
                            userId,
                            list.Id,
                            EntityLayerType.ProjectList,
                            AccessLevel.Editor,
                            CurrentUserId
                        ));
                    await UnitOfWork.Set<EntityMember>().AddRangeAsync(newMembers, cancellationToken);
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
