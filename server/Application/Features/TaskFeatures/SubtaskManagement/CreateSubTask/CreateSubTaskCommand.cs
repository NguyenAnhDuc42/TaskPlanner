namespace Application;

public record CreateSubTaskCommand(
    Guid ParentTaskId,
    string Name,
    Priority Priority,
    Guid? StatusId = null
) : ICommandRequest, IAuthorizedWorkspaceRequest;