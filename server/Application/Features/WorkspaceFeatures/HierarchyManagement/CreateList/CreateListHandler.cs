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

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.CreateList;

public class CreateListHandler : BaseCommandHandler, IRequestHandler<CreateListCommand, Guid>
{
    public CreateListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPermissionService permissionService, WorkspaceContext workspaceContext)
       : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Guid> Handle(CreateListCommand request, CancellationToken cancellationToken)
    {
        var space = await FindOrThrowAsync<ProjectSpace>(request.spaceId);

        ProjectFolder? folder = null;
        if (request.folderId.HasValue)
        {
            folder = await FindOrThrowAsync<ProjectFolder>(request.folderId.Value);

            if (folder.ProjectSpaceId != space.Id)
            {
                throw new InvalidOperationException("Folder does not belong to space");
            }
        }
        await RequirePermissionAsync(space, EntityType.ProjectList, PermissionAction.Create, cancellationToken);

        long orderKey = folder?.GetNextListOrderAndIncrement() ?? space.GetNextEntityOrderAndIncrement();
        var customization = Customization.Create(request.color, request.icon);
        var list = ProjectList.Create(
            projectSpaceId: space.Id, 
            projectFolderId: request.folderId,
            name: request.name, 
            customization: customization,
            isPrivate: request.isPrivate,
            creatorId: CurrentUserId,
            start: request.startDate,
            due: request.dueDate,
            orderKey: orderKey);

        await UnitOfWork.Set<ProjectList>().AddAsync(list, cancellationToken);

        // Create EntityMember for owner if private
        if (request.isPrivate)
        {
            var member = EntityMember.AddMember(
                userId: CurrentUserId,
                layerId: list.Id,
                layerType: EntityLayerType.ProjectList,
                accessLevel: AccessLevel.Manager,
                creatorId: CurrentUserId
            );

            await UnitOfWork.Set<EntityMember>().AddAsync(member, cancellationToken);
        }
        
        // Invite additional members if provided
        if (request.memberIdsToInvite?.Any() == true)
        {
            var validMembers = await ValidateWorkspaceMembers(request.memberIdsToInvite, cancellationToken);

            var inviteMembers = validMembers
                .Where(userId => userId != CurrentUserId)
                .Select(userId => EntityMember.AddMember(userId, list.Id, EntityLayerType.ProjectList, AccessLevel.Editor, CurrentUserId));

            await UnitOfWork.Set<EntityMember>().AddRangeAsync(inviteMembers, cancellationToken);
        }
        
        return list.Id;
    }
}
