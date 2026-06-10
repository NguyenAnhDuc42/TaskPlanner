namespace Application;

public record DeleteSubTaskCommand(
    Guid ParentTaskId,
    Guid TaskId
) : ICommandRequest, IAuthorizedWorkspaceRequest;
