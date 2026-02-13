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

namespace Application.Features.SpaceFeatures.SelfManagement.CreateSpace;

public class CreateSpaceHandler : BaseFeatureHandler, IRequestHandler<CreateSpaceCommand, Guid>
{
    public CreateSpaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
       : base(unitOfWork, currentUserService, workspaceContext) { }
       
    public async Task<Guid> Handle(CreateSpaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await FindOrThrowAsync<ProjectWorkspace>(request.workspaceId);
        var customization = Customization.Create(request.color, request.icon);
        var orderKey = workspace.GetNextItemOrderAndIncrement();
        
        var space = ProjectSpace.Create(
            projectWorkspaceId: workspace.Id,
            name: request.name,
            description: request.description,
            customization: customization,
            isPrivate: request.isPrivate,
            inheritStatus: false,
            creatorId: CurrentUserId,
            orderKey: orderKey
        );

        await UnitOfWork.Set<ProjectSpace>().AddAsync(space, cancellationToken);
        
        // Create EntityAccess for owner if private
        if (request.isPrivate)
        {
            var memberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);
            var access = EntityAccess.Create(memberId, space.Id, EntityLayerType.ProjectSpace, AccessLevel.Manager, CurrentUserId);
            await UnitOfWork.Set<EntityAccess>().AddAsync(access, cancellationToken);
        }
        
        // Invite additional members if provided
        if (request.memberIdsToInvite?.Any() == true)
        {
            var currentMemberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);
            var memberIds = await GetWorkspaceMemberIds(request.memberIdsToInvite, cancellationToken);

            var accessRecords = memberIds
                .Where(id => id != currentMemberId) // Already handled if they were in the list
                .Select(memberId => EntityAccess.Create(memberId, space.Id, EntityLayerType.ProjectSpace, AccessLevel.Editor, CurrentUserId));

            await UnitOfWork.Set<EntityAccess>().AddRangeAsync(accessRecords, cancellationToken);
        }
        
        return space.Id;
    }
}
