using System;
using System.ComponentModel.DataAnnotations;
using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Relationship;
using Domain.Entities.Support.Workspace;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.InviteMembersToChatRoom;

public class InviteMembersToChatRoomHandler : BaseFeatureHandler, IRequestHandler<InviteMembersToChatRoomCommand, Unit>
{
    public InviteMembersToChatRoomHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }
    public async Task<Unit> Handle(InviteMembersToChatRoomCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await UnitOfWork.Set<ChatRoom>().FindAsync(request.chatRoomId, cancellationToken)?? throw new KeyNotFoundException("Chat room not found.");
        cancellationToken.ThrowIfCancellationRequested();
        var chatRommMembers = UnitOfWork.Set<WorkspaceMember>()
        .Where(wm => wm.ProjectWorkspaceId == chatRoom.ProjectWorkspaceId && request.memberIds.Contains(wm.UserId))
        .ToList();
        if (chatRommMembers.Count != request.memberIds.Count) throw new ValidationException("Some members are not part of this workspace.");
        var newMembers = ChatRoomMember.AddMembers(chatRoom.Id, chatRommMembers.Select(cm => cm.UserId).ToList(), CurrentUserId);
        chatRoom.AddMembers(newMembers);
        return Unit.Value;
    }
}
