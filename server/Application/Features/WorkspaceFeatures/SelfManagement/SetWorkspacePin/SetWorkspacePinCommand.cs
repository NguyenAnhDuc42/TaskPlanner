namespace Application;

public record SetWorkspacePinCommand(Guid WorkspaceId, bool IsPinned) : ICommandRequest;


