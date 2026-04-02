using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.ChatRoomFeatures.EditChatRoom;

public class EditChatRoomHandler : BaseFeatureHandler, IRequestHandler<EditChatRoomCommand, Unit>
{
    public EditChatRoomHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) 
        : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(EditChatRoomCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await UnitOfWork.Set<ChatRoom>().Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.ChatRoomId, cancellationToken);
            
        if (chatRoom == null) throw new KeyNotFoundException($"ChatRoom {request.ChatRoomId} not found");

        chatRoom.Update(request.NewName, request.AvatarUrl, request.IsPrivate, request.IsArchived);

        // Update notification preference for current user
        var member = chatRoom.Members.FirstOrDefault(m => m.UserId == CurrentUserId);
        if (member != null)
        {
            if (request.TurnOffNotifications) member.MuteUntil(DateTimeOffset.MaxValue);
            else member.UnMute();
        }

        return Unit.Value;
    }
}
