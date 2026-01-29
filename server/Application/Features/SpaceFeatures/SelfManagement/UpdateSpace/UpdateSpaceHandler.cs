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

namespace Application.Features.SpaceFeatures.SelfManagement.UpdateSpace;

public class UpdateSpaceHandler : BaseCommandHandler, IRequestHandler<UpdateSpaceCommand, Unit>
{
    public UpdateSpaceHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(UpdateSpaceCommand request, CancellationToken cancellationToken)
    {
        var space = await FindOrThrowAsync<ProjectSpace>(request.SpaceId);

        await RequirePermissionAsync(space, PermissionAction.Edit, cancellationToken);

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
            var existingMembers = await UnitOfWork.Set<EntityMember>()
                .Where(em => em.LayerId == space.Id && em.LayerType == EntityLayerType.ProjectSpace)
                .ToListAsync(cancellationToken);

            if (!existingMembers.Any())
            {
                var ownerMember = EntityMember.AddMember(
                    CurrentUserId,
                    space.Id,
                    EntityLayerType.ProjectSpace,
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
                            space.Id,
                            EntityLayerType.ProjectSpace,
                            AccessLevel.Editor,
                            CurrentUserId
                        ));
                    await UnitOfWork.Set<EntityMember>().AddRangeAsync(newMembers, cancellationToken);
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
