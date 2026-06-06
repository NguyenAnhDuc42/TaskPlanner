namespace Application;

public record CreateSubTaskCommand(
    Guid SpaceId,
    Guid ParentTaskId,
    string Name,
    Priority Priority
) : ICommandRequest, IAuthorizedWorkspaceRequest;