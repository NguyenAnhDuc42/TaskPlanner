using System;
using Domain.Common;

namespace Domain.Entities.Support;

public class Notification : Entity
{
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public Guid RecipientId { get; private set; }
    public Guid TriggeredByUserId { get; private set; }
    public Guid RelatedEntityId { get; private set; }

    private Notification() { } // EF

    private Notification(Guid id, string message, Guid recipientId, Guid triggeredByUserId, Guid relatedEntityId)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message cannot be empty.", nameof(message));
        if (recipientId == Guid.Empty) throw new ArgumentException("RecipientId cannot be empty.", nameof(recipientId));
        if (triggeredByUserId == Guid.Empty) throw new ArgumentException("TriggeredByUserId cannot be empty.", nameof(triggeredByUserId));
        if (relatedEntityId == Guid.Empty) throw new ArgumentException("RelatedEntityId cannot be empty.", nameof(relatedEntityId));

        Message = message.Trim();
        RecipientId = recipientId;
        TriggeredByUserId = triggeredByUserId;
        RelatedEntityId = relatedEntityId;
        IsRead = false;
    }

    public static Notification Create(string message, Guid recipientId, Guid triggeredByUserId, Guid relatedEntityId)
        => new Notification(Guid.NewGuid(), message, recipientId, triggeredByUserId, relatedEntityId);

    public void MarkAsRead()
    {
        if (IsRead) return;
        IsRead = true;
        UpdateTimestamp();
    }
}
