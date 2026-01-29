using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.Support.Workspace;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.DeleteChatRoom;

public class DeleteChatRoomHandler : BaseCommandHandler, IRequestHandler<DeleteChatRoomCommand, Unit>
{
    public DeleteChatRoomHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }
    public async Task<Unit> Handle(DeleteChatRoomCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await UnitOfWork.Set<ChatRoom>().FirstOrDefaultAsync(cr => cr.Id == request.chatRoomId, cancellationToken);
        if (chatRoom == null) throw new KeyNotFoundException("Chat room not found.");
        cancellationToken.ThrowIfCancellationRequested();

        await RequirePermissionAsync(chatRoom, PermissionAction.Delete, cancellationToken);
        chatRoom.SoftDelete();
        return Unit.Value;

    }
}
