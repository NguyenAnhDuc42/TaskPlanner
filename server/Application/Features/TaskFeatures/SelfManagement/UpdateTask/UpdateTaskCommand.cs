namespace Application;

public record UpdateTaskCommand(
    Guid TaskId,
    string? Name,
    Guid? StatusId,
    Priority? Priority,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    int? StoryPoints,
    long? TimeEstimate,
    List<Guid>? AssigneeIds = null,
    string? Icon = null,
    string? Color = null
) : ICommandRequest, IAuthorizedWorkspaceRequest;


