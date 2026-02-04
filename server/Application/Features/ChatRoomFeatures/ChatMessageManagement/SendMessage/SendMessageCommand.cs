using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.ChatRoomFeatures.ChatMessageManagement.SendMessage;

public record class SendMessageCommand(Guid ChatRoomId, string Content, Guid? ReplyToMessageId) : ICommand<Unit>;
