namespace Domain.Enums.Notification;

[Flags]
public enum NotificationChanel
{
    Email = 1,
    InApp = 2,
    SMS = 4,
    Push = 8
}
