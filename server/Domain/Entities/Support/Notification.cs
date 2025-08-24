using Domain.Common;
using System;

namespace Domain.Entities.Support;

public class Notification : Entity
{
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public Guid RecipientId { get; private set; }
    public Guid TriggeredByUserId { get; private set; }
    public Guid RelatedEntityId { get; private set; }

    private Notification() { }

    private Notification(Guid id, string message, Guid recipientId, Guid triggeredByUserId, Guid relatedEntityId)
    {
        Id = id;
        Message = message;
        RecipientId = recipientId;
        TriggeredByUserId = triggeredByUserId;
        RelatedEntityId = relatedEntityId;
        IsRead = false; // Default value
    }

    public static Notification Create(string message, Guid recipientId, Guid triggeredByUserId, Guid relatedEntityId)
    {
        return new Notification(Guid.NewGuid(), message, recipientId, triggeredByUserId, relatedEntityId);
    }
}
