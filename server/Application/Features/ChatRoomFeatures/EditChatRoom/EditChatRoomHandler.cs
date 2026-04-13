using Application.Common.Errors;
using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ChatRoomFeatures.EditChatRoom;

public class EditChatRoomHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<EditChatRoomCommand>
{
    public async Task<Result> Handle(EditChatRoomCommand request, CancellationToken ct)
    {
        // AUTHORIZATION: Only Admin or Owner can edit chat room settings globally
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var chatRoom = await db.ChatRooms.Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == request.ChatRoomId, ct);
            
        if (chatRoom == null) 
            return Result.Failure(ChatRoomError.NotFound);

        chatRoom.Update(request.NewName, request.AvatarUrl, request.IsPrivate, request.IsArchived);

        // Update notification preference for current user (MemberId)
        var member = chatRoom.Members.FirstOrDefault(m => m.UserId == context.CurrentMember.UserId);
        if (member != null)
        {
            if (request.TurnOffNotifications) member.MuteUntil(DateTimeOffset.MaxValue);
            else member.UnMute();
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
