using System;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.ListFeatures.SelfManagement.CreateList;

public class CreateListHandler : BaseFeatureHandler, IRequestHandler<CreateListCommand, Guid>
{
    public CreateListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
       : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Guid> Handle(CreateListCommand request, CancellationToken cancellationToken)
    {
        ProjectSpace space;
        ProjectFolder? folder = null;

        if (request.folderId.HasValue)
        {
            folder = await FindOrThrowAsync<ProjectFolder>(request.folderId.Value);
            
            // If spaceId is empty/default, use the folder's space ID
            if (request.spaceId == Guid.Empty)
            {
                 space = await FindOrThrowAsync<ProjectSpace>(folder.ProjectSpaceId);
            }
            else
            {
                // Verify space ID matches if provided
                if (folder.ProjectSpaceId != request.spaceId)
                {
                    throw new ArgumentException("Folder does not belong to the specified Space.");
                }
                space = await FindOrThrowAsync<ProjectSpace>(request.spaceId);
            }
        }
        else
        {
            if (request.spaceId == Guid.Empty)
            {
                 throw new ArgumentException("SpaceId is required when creating a list at the root.");
            }
            space = await FindOrThrowAsync<ProjectSpace>(request.spaceId);
        }

        var customization = Customization.Create(request.color, request.icon);
        var orderKey = folder?.GetNextItemOrderAndIncrement() ?? space.GetNextItemOrderAndIncrement();

        var list = ProjectList.Create(
            projectSpaceId: space.Id,
            projectFolderId: folder?.Id,
            name: request.name,
            customization: customization,
            isPrivate: request.isPrivate,
            inheritStatus: false,
            creatorId: CurrentUserId,
            orderKey: orderKey
        );

        await UnitOfWork.Set<ProjectList>().AddAsync(list, cancellationToken);
        
        await UnitOfWork.Set<ProjectList>().AddAsync(list, cancellationToken);
        
        // Create EntityAccess for owner if private
        if (request.isPrivate)
        {
            var memberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);
            var access = EntityAccess.Create(memberId, list.Id, EntityLayerType.ProjectList, AccessLevel.Manager, CurrentUserId);
            await UnitOfWork.Set<EntityAccess>().AddAsync(access, cancellationToken);
        }
        
        // Invite additional members if provided
        if (request.memberIdsToInvite?.Any() == true)
        {
            var workspaceMemberIds = await GetWorkspaceMemberIds(request.memberIdsToInvite, cancellationToken);
            var ownerMemberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);

            var accessRecords = workspaceMemberIds
                .Where(memberId => memberId != ownerMemberId)
                .Select(memberId => EntityAccess.Create(memberId, list.Id, EntityLayerType.ProjectList, AccessLevel.Editor, CurrentUserId));

            await UnitOfWork.Set<EntityAccess>().AddRangeAsync(accessRecords, cancellationToken);
        }

        return list.Id;
    }
}
