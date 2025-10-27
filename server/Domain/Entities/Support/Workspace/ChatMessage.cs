using Domain.Common;

namespace Domain.Entities.Support.Workspace;

public class ChatMessage : Entity
{
    public Guid ChatRoomId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Content { get; private set; } = null!;
    public bool IsEdited { get; private set; } = false;
    public DateTimeOffset? EditedAt { get; private set; }
    public bool IsPinned { get; private set; } = false;
    public bool HasAttachment { get; private set; } = false;
    public Guid? ReplyToMessageId { get; private set; }
    public int ReactionCount { get; private set; } = 0;

    // Navigation properties
    public ChatRoom? ChatRoom { get; private set; }
    public ChatMessage? ReplyToMessage { get; private set; }

    private ChatMessage() { }

    private ChatMessage(Guid chatRoomId, Guid senderId, string content, Guid? replyToMessageId = null)
    {
        ChatRoomId = chatRoomId;
        SenderId = senderId;
        Content = content;
        ReplyToMessageId = replyToMessageId;
    }

    public static ChatMessage Create(Guid chatRoomId, Guid senderId, string content, Guid? replyToMessageId = null)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Message content cannot be empty.", nameof(content));

        var message = new ChatMessage(chatRoomId, senderId, content, replyToMessageId);
        // message.AddDomainEvent(new ChatMessageCreatedEvent(message.Id, chatRoomId, senderId));
        return message;
    }

    public void EditContent(string newContent, Guid userId)
    {
        if (userId != SenderId)
            throw new UnauthorizedAccessException("Only the message sender can edit this message.");

        if (string.IsNullOrWhiteSpace(newContent))
            throw new ArgumentException("Message content cannot be empty.", nameof(newContent));

        Content = newContent;
        IsEdited = true;
        EditedAt = DateTimeOffset.UtcNow;
        UpdateTimestamp();
        // AddDomainEvent(new ChatMessageEditedEvent(Id, ChatRoomId, userId));
    }

    public void Pin()
    {
        if (IsPinned)
            throw new InvalidOperationException("Message is already pinned.");

        IsPinned = true;
        UpdateTimestamp();
        // AddDomainEvent(new ChatMessagePinnedEvent(Id, ChatRoomId));
    }

    public void Unpin()
    {
        if (!IsPinned)
            throw new InvalidOperationException("Message is not pinned.");

        IsPinned = false;
        UpdateTimestamp();
        // AddDomainEvent(new ChatMessageUnpinnedEvent(Id, ChatRoomId));
    }

    public void SetHasAttachment(bool hasAttachment)
    {
        HasAttachment = hasAttachment;
        UpdateTimestamp();
    }

    public void IncrementReactionCount() => ReactionCount++;
    public void DecrementReactionCount()
    {
        if (ReactionCount > 0) ReactionCount--;
    }

    public bool CanUserDelete(Guid userId) => userId == SenderId;
    public bool CanUserEdit(Guid userId) => userId == SenderId;
}