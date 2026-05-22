namespace Application;

public record class LeaveWorkspaceCommand(Guid WorkspaceId) : ICommandRequest, IAuthorizedWorkspaceRequest;


