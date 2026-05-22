namespace Application;

public record DeleteTaskCommand(Guid TaskId) : ICommandRequest, IAuthorizedWorkspaceRequest;


