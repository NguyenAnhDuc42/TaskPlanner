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

namespace Application.Features.SpaceFeatures.SelfManagement.CreateSpace;

public class CreateSpaceHandler : BaseFeatureHandler, IRequestHandler<CreateSpaceCommand, Guid>
{
    public CreateSpaceHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
       : base(unitOfWork, currentUserService, workspaceContext) { }
       
    public async Task<Guid> Handle(CreateSpaceCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>().FindAsync(request.workspaceId, cancellationToken);
        if (workspace == null) throw new KeyNotFoundException($"Workspace {request.workspaceId} not found");
        var customization = Customization.Create(request.color, request.icon);

        // Resolve order key: append after the last space in this workspace
        var maxKey = await UnitOfWork.Set<ProjectSpace>()
            .Where(s => s.ProjectWorkspaceId == request.workspaceId && s.DeletedAt == null)
            .MaxAsync(s => (string?)s.OrderKey, cancellationToken);
        var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
        
        var space = ProjectSpace.Create(
            projectWorkspaceId: workspace.Id,
            name: request.name,
            description: request.description,
            customization: customization,
            isPrivate: request.isPrivate,
            creatorId: CurrentUserId,
            orderKey: orderKey
        );

        await UnitOfWork.Set<ProjectSpace>().AddAsync(space, cancellationToken);
        
        // Create EntityAccess for owner if private
        if (request.isPrivate)
        {
            var memberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);
            var access = EntityAccess.Create(WorkspaceId, memberId, space.Id, EntityLayerType.ProjectSpace, AccessLevel.Manager, CurrentUserId);
            await UnitOfWork.Set<EntityAccess>().AddAsync(access, cancellationToken);
        }
        
        // Invite additional members if provided
        if (request.memberIdsToInvite?.Any() == true)
        {
            var currentMemberId = await GetWorkspaceMemberId(CurrentUserId, cancellationToken);
            var memberIds = await GetWorkspaceMemberIds(request.memberIdsToInvite, cancellationToken);

            var accessRecords = memberIds
                .Where(id => id != currentMemberId) // Already handled if they were in the list
                .Select(memberId => EntityAccess.Create(WorkspaceId, memberId, space.Id, EntityLayerType.ProjectSpace, AccessLevel.Editor, CurrentUserId));

            await UnitOfWork.Set<EntityAccess>().AddRangeAsync(accessRecords, cancellationToken);
        }
        
        return space.Id;
    }
}
