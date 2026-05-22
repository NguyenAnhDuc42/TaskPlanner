namespace Application;

public record DeleteWorkspaceCommand(Guid workspaceId) : ICommandRequest, IAuthorizedWorkspaceRequest;


