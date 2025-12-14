using System;
using Domain.Common;
using Domain.Enums.Notification;

namespace Domain.Entities.Support.Notification;

public class UserNotification : Composite
{
    public Guid UserId { get; private set; }
    public Guid NotficationEventId { get; private set; }
    public NotificationStatus Status { get; private set; } = NotificationStatus.Unread;
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public NotificationChanel Chanel { get; private set; } = NotificationChanel.Email | NotificationChanel.InApp;

    private UserNotification() { } //Ef
    private UserNotification(Guid userId, Guid notificationEventId, NotificationChanel chanel)
    {
        UserId = userId;
        NotficationEventId = notificationEventId;
        Chanel = chanel;
    }

}
