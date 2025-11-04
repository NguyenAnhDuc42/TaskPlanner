using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Domain.Entities.Support.Workspace;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.RemoveMembersFromChatRoom;

public class RemoveMembersFromChatRoomHandler : BaseCommandHandler, IRequestHandler<RemoveMembersFromChatRoomCommand, Unit>
{
    public RemoveMembersFromChatRoomHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(RemoveMembersFromChatRoomCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await UnitOfWork.Set<ChatRoom>().FindAsync(request.chatRoomId,cancellationToken) ?? throw new KeyNotFoundException("Chat room not found.");
        cancellationToken.ThrowIfCancellationRequested();
        await RequirePermissionAsync(chatRoom,EntityType.ChatRoomMember,PermissionAction.Delete, cancellationToken);
        chatRoom.RemoveMembers(request.memberIds);
        return Unit.Value;
    }
}
