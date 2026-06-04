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
    string? Icon = null,
    string? Color = null
) : ICommandRequest, IAuthorizedWorkspaceRequest;
