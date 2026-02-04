using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.Relationship;
using Domain.Entities.Support.Workspace;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.ChatRoomFeatures.RemoveMemberFromChatRoom;

public class RemoveMemberFromChatRoomHandler : BaseFeatureHandler, IRequestHandler<RemoveMembersFromChatRoomCommand, Unit>
{
    public RemoveMemberFromChatRoomHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(RemoveMembersFromChatRoomCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await FindOrThrowAsync<ChatRoom>(request.chatRoomId);
        
        var membersToRemove = await UnitOfWork.Set<ChatRoomMember>()
            .Where(x => x.ChatRoomId == chatRoom.Id && request.memberIds.Contains(x.UserId))
            .ToListAsync(cancellationToken);

        UnitOfWork.Set<ChatRoomMember>().RemoveRange(membersToRemove);

        return Unit.Value;
    }
}
