namespace Application;

public record CreateTaskCommand(
    Guid ParentId,
    EntityLayerType ParentType,
    string Name,
    Guid? StatusId,
    Priority Priority,
    List<Guid>? AssigneeIds,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate,
    int? StoryPoints,
    long? TimeEstimate,
    string? Icon = null,
    string? Color = null
) : ICommandRequest<Guid>, IAuthorizedWorkspaceRequest;
