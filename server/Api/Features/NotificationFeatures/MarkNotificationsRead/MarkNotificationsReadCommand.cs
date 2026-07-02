namespace Api;

// Ids = null means mark ALL as read. Not IAuthorizedWorkspaceRequest — notifications span
// all of the user's workspaces.
public record MarkNotificationsReadCommand(List<Guid>? Ids = null) : ICommandRequest;
