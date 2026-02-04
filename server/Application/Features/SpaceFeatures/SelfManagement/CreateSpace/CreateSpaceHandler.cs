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
        
        // Create EntityMember for owner if private
        if (request.isPrivate)
        {
            var member = EntityMember.AddMember(CurrentUserId, space.Id, EntityLayerType.ProjectSpace, AccessLevel.Manager, CurrentUserId);
            await UnitOfWork.Set<EntityMember>().AddAsync(member, cancellationToken);
        }
        
        // Invite additional members if provided
        if (request.memberIdsToInvite?.Any() == true)
        {
            var validMembers = await ValidateWorkspaceMembers(request.memberIdsToInvite, cancellationToken);

            var inviteMembers = validMembers
                .Where(userId => userId != CurrentUserId)
                .Select(userId => EntityMember.AddMember(userId, space.Id, EntityLayerType.ProjectSpace, AccessLevel.Editor, CurrentUserId));

            await UnitOfWork.Set<EntityMember>().AddRangeAsync(inviteMembers, cancellationToken);
        }
        
        return space.Id;
    }
}
