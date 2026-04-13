using Application.Common.Interfaces;

namespace Application.Features.ChatRoomFeatures.ChatMessageManagement.SendMessage;

public record SendMessageCommand(Guid ChatRoomId, string Content, Guid? ReplyToMessageId) : ICommandRequest;
