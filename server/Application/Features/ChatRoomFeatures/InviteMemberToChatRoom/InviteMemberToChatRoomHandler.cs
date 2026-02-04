using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.Relationship;
using Domain.Entities.Support.Workspace;
using Domain.Enums.Workspace;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.ChatRoomFeatures.InviteMemberToChatRoom;

public class InviteMemberToChatRoomHandler : BaseFeatureHandler, IRequestHandler<InviteMembersToChatRoomCommand, Unit>
{
    public InviteMemberToChatRoomHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(InviteMembersToChatRoomCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await FindOrThrowAsync<ChatRoom>(request.chatRoomId);
        
        var chatRoomMembers = request.memberIds
            .Select(x => ChatRoomMember.AddMember(chatRoom.Id, x, CurrentUserId));
            
        await UnitOfWork.Set<ChatRoomMember>().AddRangeAsync(chatRoomMembers, cancellationToken);
        
        return Unit.Value;
    }
}
