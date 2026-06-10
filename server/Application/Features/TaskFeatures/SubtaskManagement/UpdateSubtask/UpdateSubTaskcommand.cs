namespace Application;

public record UpdateSubTaskCommand(
    Guid ParentTaskId,
    Guid TaskId,
    string? Name,
    Priority? Priority
): ICommandRequest, IAuthorizedWorkspaceRequest;
