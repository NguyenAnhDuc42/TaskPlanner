using System;
using Domain.Common;

namespace Domain.Entities.Support.Workspace;

public class ChatMessage : Entity
{
    public Guid ChatRoomId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Content { get; private set; } = null!;
    public bool IsEdited { get; private set; } = false;
    public bool IsPinned { get; private set; } = false;
    public bool HasAttachment { get; private set; } = false;
    public Guid? ReplyToMessageId { get; private set; }

}
