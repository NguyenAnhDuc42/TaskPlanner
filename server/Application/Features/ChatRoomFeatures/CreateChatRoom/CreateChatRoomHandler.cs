using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Workspace;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.ChatRoomFeatures.CreateChatRoom;

public class CreateChatRoomHandler : BaseFeatureHandler, IRequestHandler<CreateChatRoomCommand, Unit>
{
    public CreateChatRoomHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(CreateChatRoomCommand request, CancellationToken cancellationToken)
    {
        var workspace = await FindOrThrowAsync<ProjectWorkspace>(request.workspaceId);
        
        var chatRoom = ChatRoom.Create(
            name: request.name, 
            projectWorkspaceId: workspace.Id, 
            creatorId: CurrentUserId,
            isPrivate: request.inviteMembersInWorkspace, // This seems to be mapped to inviteMembersInWorkspace in command? check consistency
            avatarUrl: request.avatarUrl
        );
        
        await UnitOfWork.Set<ChatRoom>().AddAsync(chatRoom, cancellationToken);
        
        // creator is added as owner inside ChatRoom.Create via navigation property

        if (request.memberIds != null && request.memberIds.Any())
        {
            var chatRoomMembers = request.memberIds
                .Where(id => id != CurrentUserId)
                .Select(x => ChatRoomMember.AddMember(chatRoom.Id, x, CurrentUserId));
                
            await UnitOfWork.Set<ChatRoomMember>().AddRangeAsync(chatRoomMembers, cancellationToken);
        }

        return Unit.Value;
    }
}
