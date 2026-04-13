using Application.Common.Errors;
using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ChatRoomFeatures.RemoveMemberFromChatRoom;

public class RemoveMemberFromChatRoomHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<RemoveMembersFromChatRoomCommand>
{
    public async Task<Result> Handle(RemoveMembersFromChatRoomCommand request, CancellationToken ct)
    {
        // AUTHORIZATION: Only Admin or Owner can remove members from chat rooms
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var chatRoom = await db.ChatRooms.FirstOrDefaultAsync(x => x.Id == request.chatRoomId, ct);
        if (chatRoom == null) 
            return Result.Failure(ChatRoomError.NotFound);
        
        // Security Resolve: Ensure chat room belongs to authorized workspace
        if (chatRoom.ProjectWorkspaceId != context.workspaceId)
            return Result.Failure(MemberError.DontHavePermission);
        
        var membersToRemove = await db.ChatRoomMembers
            .Where(x => x.ChatRoomId == chatRoom.Id && request.memberIds.Contains(x.UserId))
            .ToListAsync(ct);

        if (membersToRemove.Any())
        {
            db.ChatRoomMembers.RemoveRange(membersToRemove);
            await db.SaveChangesAsync(ct);
        }

        return Result.Success();
    }
}
