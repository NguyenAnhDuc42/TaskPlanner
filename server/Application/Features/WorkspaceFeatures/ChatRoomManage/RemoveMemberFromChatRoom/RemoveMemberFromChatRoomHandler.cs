using System;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.Support.Workspace;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.RemoveMembersFromChatRoom;

public class RemoveMembersFromChatRoomHandler : BaseFeatureHandler, IRequestHandler<RemoveMembersFromChatRoomCommand, Unit>
{
    public RemoveMembersFromChatRoomHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(RemoveMembersFromChatRoomCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await UnitOfWork.Set<ChatRoom>().FindAsync(request.chatRoomId,cancellationToken) ?? throw new KeyNotFoundException("Chat room not found.");
        cancellationToken.ThrowIfCancellationRequested();
        chatRoom.RemoveMembers(request.memberIds);
        return Unit.Value;
    }
}
