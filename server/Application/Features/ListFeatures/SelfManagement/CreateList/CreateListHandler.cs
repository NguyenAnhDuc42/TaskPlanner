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
        
        // Create EntityMember for owner if private
        if (request.isPrivate)
        {
            var member = EntityMember.AddMember(CurrentUserId, list.Id, EntityLayerType.ProjectList, AccessLevel.Manager, CurrentUserId);
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
