namespace Application;

public record DeleteSubTaskCommand(
    Guid SpaceId,
    Guid ParentTaskId,
    Guid TaskId
) : ICommandRequest, IAuthorizedWorkspaceRequest;
