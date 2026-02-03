using System;
using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Support.Workspace;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.DeleteChatRoom;

public class DeleteChatRoomHandler : BaseFeatureHandler, IRequestHandler<DeleteChatRoomCommand, Unit>
{
    public DeleteChatRoomHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    : base(unitOfWork, currentUserService, workspaceContext) { }
    public async Task<Unit> Handle(DeleteChatRoomCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await UnitOfWork.Set<ChatRoom>().FirstOrDefaultAsync(cr => cr.Id == request.chatRoomId, cancellationToken);
        if (chatRoom == null) throw new KeyNotFoundException("Chat room not found.");
        cancellationToken.ThrowIfCancellationRequested();
        chatRoom.SoftDelete();
        return Unit.Value;

    }
}
