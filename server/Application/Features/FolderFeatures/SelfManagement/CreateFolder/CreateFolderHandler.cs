using System;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Common;
using Domain.Entities.ProjectEntities;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.FolderFeatures.SelfManagement.CreateFolder;

public class CreateFolderHandler : BaseFeatureHandler, IRequestHandler<CreateFolderCommand, Guid>
{
    public CreateFolderHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
       : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Guid> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        var space = await UnitOfWork.Set<ProjectSpace>().FindAsync(request.spaceId, cancellationToken);
        if (space == null) throw new KeyNotFoundException($"Space {request.spaceId} not found");
        
        var customization = Customization.Create(request.color, request.icon);

        // Resolve order key: append after the last folder in this space
        var maxKey = await UnitOfWork.Set<ProjectFolder>()
            .Where(f => f.ProjectSpaceId == request.spaceId && f.DeletedAt == null)
            .MaxAsync(f => (string?)f.OrderKey, cancellationToken);
        var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
        
        var folder = ProjectFolder.Create(
            projectSpaceId: space.Id,
            name: request.name,
            color: request.color,
            icon: request.icon,
            isPrivate: request.isPrivate,
            creatorId: CurrentUserId,
            orderKey: orderKey,
            start: request.startDate,
            due: request.dueDate
        );

        await UnitOfWork.Set<ProjectFolder>().AddAsync(folder, cancellationToken);

        // Create EntityAccess for owner if private
        if (request.isPrivate)
        {
            var memberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);
            var access = EntityAccess.Create(WorkspaceId, memberId, folder.Id, EntityLayerType.ProjectFolder, AccessLevel.Manager, CurrentUserId);
            await UnitOfWork.Set<EntityAccess>().AddAsync(access, cancellationToken);
        }
        
        // Invite additional members if provided
        if (request.memberIdsToInvite?.Any() == true)
        {
            // Resolve workspace member IDs
            var workspaceMemberIds = await GetWorkspaceMemberIds(request.memberIdsToInvite, cancellationToken);
            var ownerMemberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);
            
            var inviteAccessRecords = workspaceMemberIds
                .Where(memberId => memberId != ownerMemberId)
                .Select(memberId => EntityAccess.Create(WorkspaceId, memberId, folder.Id, EntityLayerType.ProjectFolder, AccessLevel.Editor, CurrentUserId));

            await UnitOfWork.Set<EntityAccess>().AddRangeAsync(inviteAccessRecords, cancellationToken);
        }
        
        return folder.Id;
    }
}
