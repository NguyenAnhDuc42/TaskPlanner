using Application.Common.Errors;
using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ChatRoomFeatures.DeleteChatRoom;

public class DeleteChatRoomHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<DeleteChatRoomCommand>
{
    public async Task<Result> Handle(DeleteChatRoomCommand request, CancellationToken ct)
    {
        // AUTHORIZATION: Only Admin or Owner can delete chat rooms
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var chatRoom = await db.ChatRooms.FirstOrDefaultAsync(x => x.Id == request.ChatRoomId, ct);
        if (chatRoom == null) 
            return Result.Failure(ChatRoomError.NotFound);
        
        chatRoom.SoftDelete();
        await db.SaveChangesAsync(ct);
        
        return Result.Success();
    }
}
