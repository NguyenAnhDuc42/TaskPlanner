namespace Application;

public record CreateSubTaskCommand(
    Guid ParentTaskId,
    string Name,
    Priority Priority
) : ICommandRequest, IAuthorizedWorkspaceRequest;