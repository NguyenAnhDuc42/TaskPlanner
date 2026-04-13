using Application.Common.Interfaces;

namespace Application.Features.ChatRoomFeatures.ChatMessageManagement.GetMessages;

public record GetMessagesQuery(Guid ChatRoomId, int Limit, Guid? BeforeMessageId) : IQueryRequest<List<MessageDto>>;

public record MessageDto(Guid id, Guid userId, string content, DateTime createdAt, Guid? replyToMessageId);
