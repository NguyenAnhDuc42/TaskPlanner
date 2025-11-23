using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.ProjectEntities;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.CreateFolder;

public class CreateFolderHandler : BaseCommandHandler, IRequestHandler<CreateFolderCommand, Guid>
{
    public CreateFolderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService, WorkspaceContext workspaceContext)
       : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Guid> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        var space = await FindOrThrowAsync<ProjectSpace>(request.spaceId);
        
        await RequirePermissionAsync(space, EntityType.ProjectFolder, PermissionAction.Create, cancellationToken);
        var customization = Customization.Create(request.color, request.icon);
        var orderKey = space.GetNextEntityOrderAndIncrement();
        
        var folder = ProjectFolder.Create(
            projectSpaceId: space.Id,
            name: request.name,
            color: customization.Color,
            icon: customization.Icon,
            isPrivate: request.isPrivate,
            creatorId: CurrentUserId,
            orderKey: orderKey
        );

        await UnitOfWork.Set<ProjectFolder>().AddAsync(folder, cancellationToken);
        
        // Create EntityMember for owner if private
        if (request.isPrivate)
        {
            var member = EntityMember.AddMember(CurrentUserId, folder.Id, EntityLayerType.ProjectFolder, AccessLevel.Manager, CurrentUserId);
            await UnitOfWork.Set<EntityMember>().AddAsync(member, cancellationToken);
        }
        
        // Invite additional members if provided
        if (request.memberIdsToInvite?.Any() == true)
        {
            // Validate all are workspace members
            var validMembers = await ValidateWorkspaceMembers(request.memberIdsToInvite, cancellationToken);

            // Create EntityMembers for invitees (exclude owner to avoid duplicates)
            var inviteMembers = validMembers
                .Where(userId => userId != CurrentUserId)
                .Select(userId => EntityMember.AddMember(userId, folder.Id, EntityLayerType.ProjectFolder, AccessLevel.Editor, CurrentUserId));

            await UnitOfWork.Set<EntityMember>().AddRangeAsync(inviteMembers, cancellationToken);
        }
        
        return folder.Id;
    }
}
