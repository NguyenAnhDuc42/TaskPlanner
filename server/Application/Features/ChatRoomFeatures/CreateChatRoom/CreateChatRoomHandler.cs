using Application.Common.Errors;
using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Application.Features;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ChatRoomFeatures.CreateChatRoom;

public class CreateChatRoomHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<CreateChatRoomCommand>
{
    public async Task<Result> Handle(CreateChatRoomCommand request, CancellationToken ct)
    {
        // AUTHORIZATION: Only Members or above can create chat rooms
        if (context.CurrentMember.Role > Role.Member)
            return Result.Failure(MemberError.DontHavePermission);

        var workspace = await db.Workspaces
            .ById(request.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) 
            return Result.Failure(WorkspaceError.NotFound);
        
        var chatRoom = ChatRoom.Create(
            name: request.name, 
            projectWorkspaceId: workspace.Id, 
            creatorId: context.CurrentMember.Id, // MemberId
            isPrivate: request.inviteMembersInWorkspace, 
            avatarUrl: request.avatarUrl
        );
        
        await db.ChatRooms.AddAsync(chatRoom, ct);
        
        // Handle initial members - they MUST be members of this workspace
        if (request.memberIds != null && request.memberIds.Any())
        {
            // Verify that all invited users are actually members of this workspace using Domain Extensions
            var validWorkspaceMembers = await db.WorkspaceMembers
                .ByWorkspace(workspace.Id)
                .Where(m => request.memberIds.Contains(m.UserId))
                .Select(m => m.UserId)
                .ToListAsync(ct);

            var chatRoomMembers = validWorkspaceMembers
                .Where(userId => userId != context.CurrentMember.UserId)
                .Select(userId => ChatRoomMember.AddMember(chatRoom.Id, userId, context.CurrentMember.Id))
                .ToList();
                
            if (chatRoomMembers.Any())
            {
                await db.ChatRoomMembers.AddRangeAsync(chatRoomMembers, ct);
            }
        }

        // Add the creator as the owner/member as well
        var creatorMembership = ChatRoomMember.AddOwner(chatRoom.Id, context.CurrentMember.UserId, context.CurrentMember.Id);
        await db.ChatRoomMembers.AddAsync(creatorMembership, ct);

        await db.SaveChangesAsync(ct);
        
        return Result.Success();
    }
}
