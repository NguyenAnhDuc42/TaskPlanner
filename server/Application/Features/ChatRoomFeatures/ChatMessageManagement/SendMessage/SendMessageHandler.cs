using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.ChatRoomFeatures.ChatMessageManagement.SendMessage;

public class SendMessageHandler : BaseFeatureHandler, IRequestHandler<SendMessageCommand, Unit>
{
    public SendMessageHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await UnitOfWork.Set<ChatRoom>().FindAsync(request.ChatRoomId, cancellationToken);
        if (chatRoom == null) throw new KeyNotFoundException($"ChatRoom {request.ChatRoomId} not found");
        var message = ChatMessage.Create(chatRoom.Id, CurrentUserId, request.Content, request.ReplyToMessageId);
        await UnitOfWork.Set<ChatMessage>().AddAsync(message, cancellationToken);
        return Unit.Value;
    }
}
