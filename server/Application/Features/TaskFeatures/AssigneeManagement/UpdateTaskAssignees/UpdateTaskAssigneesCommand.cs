namespace Application;

public record AssigneeChangeValue(Guid MemberId, bool IsDelete);

public record UpdateTaskAssigneesCommand(
    Guid TaskId,
    List<AssigneeChangeValue> Changes
) : ICommandRequest, IAuthorizedWorkspaceRequest;
