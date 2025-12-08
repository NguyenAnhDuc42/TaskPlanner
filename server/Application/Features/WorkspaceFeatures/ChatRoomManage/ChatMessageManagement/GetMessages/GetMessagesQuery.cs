using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.ChatMessageManagement.GetMessages;

public record GetMessagesQuery(
    Guid ChatRoomId,
    int Limit = 50,
    Guid? BeforeMessageId = null  // For cursor-based pagination
) : IQuery<GetMessagesResult>;

public record GetMessagesResult(
    List<MessageDto> Messages,
    bool HasMore
);

public record MessageDto(
    Guid Id,
    Guid SenderId,
    string Content,
    bool IsEdited,
    DateTimeOffset? EditedAt,
    bool IsPinned,
    Guid? ReplyToMessageId,
    int ReactionCount,
    DateTimeOffset CreatedAt
);
