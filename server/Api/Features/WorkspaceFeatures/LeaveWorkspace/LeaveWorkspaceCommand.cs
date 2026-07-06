namespace Api;

public record class LeaveWorkspaceCommand(Guid WorkspaceId) : ICommandRequest, IAuthorizedWorkspaceRequest;
