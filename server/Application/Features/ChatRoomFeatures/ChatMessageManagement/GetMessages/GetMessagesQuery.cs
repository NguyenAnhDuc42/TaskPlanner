using MediatR;

namespace Application.Features.ChatRoomFeatures.ChatMessageManagement.GetMessages;

public record class GetMessagesQuery(Guid ChatRoomId, int Limit, Guid? BeforeMessageId) : IRequest<List<MessageDto>>;

public record class MessageDto(Guid id, Guid userId, string content, DateTime createdAt, Guid? replyToMessageId);
