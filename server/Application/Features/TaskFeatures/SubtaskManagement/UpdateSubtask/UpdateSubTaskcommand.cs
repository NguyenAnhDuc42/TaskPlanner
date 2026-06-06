namespace Application;

public record UpdateSubTaskCommand(
    Guid SpaceId,
    Guid ParentTaskId,
    Guid TaskId,
    string? Name,
    Priority? Priority
): ICommandRequest, IAuthorizedWorkspaceRequest;
