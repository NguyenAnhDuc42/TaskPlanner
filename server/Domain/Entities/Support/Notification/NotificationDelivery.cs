using System;
using Domain.Common;
using Domain.Enums.Notification;

namespace Domain.Entities.Support.Notification;

public class NotificationDelivery : Entity
{
    public Guid UserNotificationId { get; private set; }
    public NotificationChanel Chanel { get; private set; }
    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Pending;
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public int RetryCount { get; private set; } = 0;
    public DateTimeOffset? LastRetryAt { get; private set; }
    public string? Metadate { get; private set; } = string.Empty;

    private NotificationDelivery() { } // EF
    private NotificationDelivery(Guid userNotificationId, NotificationChanel chanel)
    {
        UserNotificationId = userNotificationId;
        Chanel = chanel;
    }
}
