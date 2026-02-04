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

namespace Application.Features.FolderFeatures.SelfManagement.CreateFolder;

public class CreateFolderHandler : BaseFeatureHandler, IRequestHandler<CreateFolderCommand, Guid>
{
    public CreateFolderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
       : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Guid> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        var space = await FindOrThrowAsync<ProjectSpace>(request.spaceId);
        
        var customization = Customization.Create(request.color, request.icon);
        var orderKey = space.GetNextItemOrderAndIncrement();
        
        var folder = ProjectFolder.Create(
            projectSpaceId: space.Id,
            name: request.name,
            color: customization.Color,
            icon: customization.Icon,
            isPrivate: request.isPrivate,
            inheritStatus: false,
            creatorId: CurrentUserId,
            orderKey: orderKey
        );

        await UnitOfWork.Set<ProjectFolder>().AddAsync(folder, cancellationToken);
        
        await UnitOfWork.Set<ProjectFolder>().AddAsync(folder, cancellationToken);
        
        // Create EntityAccess for owner if private
        if (request.isPrivate)
        {
            var memberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);
            var access = EntityAccess.Create(memberId, folder.Id, EntityLayerType.ProjectFolder, AccessLevel.Manager, CurrentUserId);
            await UnitOfWork.Set<EntityAccess>().AddAsync(access, cancellationToken);
        }
        
        // Invite additional members if provided
        if (request.memberIdsToInvite?.Any() == true)
        {
            // Resolve workspace member IDs
            var workspaceMemberIds = await GetWorkspaceMemberIds(request.memberIdsToInvite, cancellationToken);

            // Create EntityAccess for invitees (exclude owner to avoid duplicates)
            var accessRecords = workspaceMemberIds
                .Where(id => id != CurrentUserId) // This is wrong, it should be where wm.UserId != CurrentUserId. Let's fix GetWorkspaceMemberIds to return a dict or something if needed, or just resolve manually here.
                // Wait, GetWorkspaceMemberIds returns wm.Id. CurrentUserId is User.Id.
                // I need the wm.Id for CurrentUserId.
                ;
                
            var ownerMemberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);
            
            var inviteAccessRecords = workspaceMemberIds
                .Where(memberId => memberId != ownerMemberId)
                .Select(memberId => EntityAccess.Create(memberId, folder.Id, EntityLayerType.ProjectFolder, AccessLevel.Editor, CurrentUserId));

            await UnitOfWork.Set<EntityAccess>().AddRangeAsync(inviteAccessRecords, cancellationToken);
        }
        
        return folder.Id;
    }
}
