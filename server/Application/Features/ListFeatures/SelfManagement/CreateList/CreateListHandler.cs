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
        var space = await FindOrThrowAsync<ProjectSpace>(request.spaceId);
        
        ProjectFolder? folder = null;
        if (request.folderId.HasValue)
        {
            folder = await FindOrThrowAsync<ProjectFolder>(request.folderId.Value);
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
