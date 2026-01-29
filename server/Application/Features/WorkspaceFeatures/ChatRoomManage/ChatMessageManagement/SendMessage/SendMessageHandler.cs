using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.Support.Workspace;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.ChatMessageManagement.SendMessage;

public class SendMessageHandler : BaseCommandHandler, IRequestHandler<SendMessageCommand, SendMessageResult>
{
    private readonly IRealtimeService _realtime;

    public SendMessageHandler(
        IUnitOfWork unitOfWork,
        IPermissionService permissionService,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        IRealtimeService realtime)
        : base(unitOfWork, permissionService, currentUserService, workspaceContext)
    {
        _realtime = realtime;
    }

    public async Task<SendMessageResult> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await UnitOfWork.Set<ChatRoom>()
            .Include(cr => cr.Members)
            .FirstOrDefaultAsync(cr => cr.Id == request.ChatRoomId && cr.DeletedAt == null, cancellationToken)
            ?? throw new KeyNotFoundException("Chat room not found.");

        // Check user is member of this chat room
        if (!chatRoom.IsMember(CurrentUserId))
            throw new UnauthorizedAccessException("You are not a member of this chat room.");

        // Create message
        var message = ChatMessage.Create(
            request.ChatRoomId,
            CurrentUserId,
            request.Content,
            request.ReplyToMessageId
        );

        chatRoom.AddMessage(message);

        // Notify other members via SignalR (fire-and-forget)
        _ = _realtime.NotifyChatRoomAsync(request.ChatRoomId, "NewMessage", new
        {
            MessageId = message.Id,
            SenderId = CurrentUserId,
            Content = message.Content,
            SentAt = message.CreatedAt,
            ReplyToMessageId = message.ReplyToMessageId
        }, cancellationToken);

        return new SendMessageResult(message.Id, message.CreatedAt);
    }
}
