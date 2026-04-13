using Application.Common.Errors;
using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ChatRoomFeatures.InviteMemberToChatRoom;

public class InviteMemberToChatRoomHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<InviteMembersToChatRoomCommand>
{
    public async Task<Result> Handle(InviteMembersToChatRoomCommand request, CancellationToken ct)
    {
        // AUTHORIZATION: Only Admin or Owner can invite members to chat rooms
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var chatRoom = await db.ChatRooms.FirstOrDefaultAsync(x => x.Id == request.chatRoomId, ct);
        if (chatRoom == null) 
            return Result.Failure(ChatRoomError.NotFound);
        
        // Security Resolve: Ensure chat room belongs to the authorized workspace
        if (chatRoom.ProjectWorkspaceId != context.workspaceId)
            return Result.Failure(MemberError.DontHavePermission);

        // TENANCY VALIDATION: Ensure all invited members are actually members of this workspace
        var validWorkspaceMemberUserIds = await db.WorkspaceMembers
            .Where(m => m.ProjectWorkspaceId == context.workspaceId && request.memberIds.Contains(m.UserId))
            .Select(m => m.UserId)
            .ToListAsync(ct);

        if (!validWorkspaceMemberUserIds.Any())
            return Result.Failure(ChatRoomError.NoValidMembersToInvite);

        // Filter out existing chat room members
        var existingChatRoomUserIds = await db.ChatRoomMembers
            .Where(m => m.ChatRoomId == chatRoom.Id)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        var newMemberUserIds = validWorkspaceMemberUserIds
            .Where(uid => !existingChatRoomUserIds.Contains(uid))
            .ToList();

        if (!newMemberUserIds.Any())
            return Result.Success(); // Or specific error if preferred

        var chatRoomMembers = newMemberUserIds
            .Select(uid => ChatRoomMember.AddMember(chatRoom.Id, uid, context.CurrentMember.Id))
            .ToList();
            
        await db.ChatRoomMembers.AddRangeAsync(chatRoomMembers, ct);
        await db.SaveChangesAsync(ct);
        
        // TODO: Move to domain event for realtime sync
        return Result.Success();
    }
}
