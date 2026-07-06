namespace Api;

public record SetWorkspacePinCommand(Guid WorkspaceId, bool IsPinned) : ICommandRequest<bool>;
