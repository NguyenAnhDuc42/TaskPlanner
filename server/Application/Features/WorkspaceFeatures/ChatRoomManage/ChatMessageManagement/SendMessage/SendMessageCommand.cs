using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.ChatMessageManagement.SendMessage;

public record SendMessageCommand(
    Guid ChatRoomId,
    string Content,
    Guid? ReplyToMessageId = null
) : ICommand<SendMessageResult>;

public record SendMessageResult(
    Guid MessageId,
    DateTimeOffset SentAt
);
