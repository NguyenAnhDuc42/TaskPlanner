namespace Application;

// Ids = null means mark ALL as read
public record MarkNotificationsReadCommand(List<Guid>? Ids = null) : ICommandRequest;
