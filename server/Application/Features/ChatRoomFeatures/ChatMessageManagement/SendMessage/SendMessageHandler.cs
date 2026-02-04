using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.Support.Workspace;
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
        var chatRoom = await FindOrThrowAsync<ChatRoom>(request.ChatRoomId);
        var message = ChatMessage.Create(chatRoom.Id, CurrentUserId, request.Content, request.ReplyToMessageId);
        await UnitOfWork.Set<ChatMessage>().AddAsync(message, cancellationToken);
        return Unit.Value;
    }
}
