using Application.Common.Errors;
using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Application.Features;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ChatRoomFeatures.ChatMessageManagement.SendMessage;

public class SendMessageHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<SendMessageCommand>
{
    public async Task<Result> Handle(SendMessageCommand request, CancellationToken ct)
    {
        // 1. Permission Check via Domain Extension
        var chatRoom = await db.ChatRooms
            .ById(request.ChatRoomId)
            .FirstOrDefaultAsync(ct);

        if (chatRoom == null) 
            return Result.Failure(ChatRoomError.NotFound);

        // 2. Identity Check (Tenant Isolation)
        // Ensure the sender is a member of this workspace chatroom
        // var isMember = await db.ChatRoomMembers
        //     .AnyAsync(m => m.ChatRoomId == chatRoom.Id && m.WorkspaceMemberId == context.CurrentMember.Id, ct);
        
        // if (!isMember)
        //     return Result.Failure(MemberError.DontHavePermission);

        var message = ChatMessage.Create(
            chatRoomId: chatRoom.Id, 
            creatorId: context.CurrentMember.Id, // Corrected parameter name from 'authorId' to 'creatorId'
            content: request.Content, 
            replyToMessageId: request.ReplyToMessageId
        );

        await db.ChatMessages.AddAsync(message, ct);
        await db.SaveChangesAsync(ct);
        
        return Result.Success();
    }
}
