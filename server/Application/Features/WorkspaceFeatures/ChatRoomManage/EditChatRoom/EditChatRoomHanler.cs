using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.Relationship;
using Domain.Entities.Support.Workspace;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.EditChatRoom;

public class EditChatRoomHanler : BaseCommandHandler, IRequestHandler<EditChatRoomCommand, Unit>
{
    public EditChatRoomHanler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }
    public async Task<Unit> Handle(EditChatRoomCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await UnitOfWork.Set<ChatRoom>().FindAsync(request.chatRoomId, cancellationToken) ?? throw new KeyNotFoundException("Chat room not found.");
        cancellationToken.ThrowIfCancellationRequested();
        await RequirePermissionAsync(chatRoom, PermissionAction.Edit, cancellationToken);

        chatRoom.Update(
            name: request.newName,
            avatarUrl: request.avatarUrl,
            isPrivate: request.isPrivate,
            isArchived: request.isArchived
        );
        if (request.turnOffNotifications)
        {
            var user = await UnitOfWork.Set<ChatRoomMember>().FirstOrDefaultAsync(cm => cm.ChatRoomId == request.chatRoomId && cm.UserId == CurrentUserId, cancellationToken);
            if (user != null) user.TurnOffNotifications();
        }
        return Unit.Value;
    }
}
