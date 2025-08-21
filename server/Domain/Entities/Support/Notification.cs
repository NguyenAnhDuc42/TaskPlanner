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

    public static Notification Create(string message, Guid recipientId, Guid triggeredByUserId, Guid relatedEntityId)
    {
        var notification = new Notification
        {
            Message = message,
            IsRead = false,
            RecipientId = recipientId,
            TriggeredByUserId = triggeredByUserId,
            RelatedEntityId = relatedEntityId
        };
        return notification;
    }
}
