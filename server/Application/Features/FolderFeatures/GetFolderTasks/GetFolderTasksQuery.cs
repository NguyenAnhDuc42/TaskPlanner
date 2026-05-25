namespace Application;

public record TaskFilter(
    List<Guid>? StatusIds = null,
    List<Priority>? Priorities = null,
    List<Guid>? AssigneeIds = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? DueDate = null,
    string? Search = null
);

public record GetFolderTasksQuery(
    Guid FolderId = default,
    string? Cursor = null,
    int Limit = 50,
    TaskFilter? Filter = null
) : IQueryRequest<PagedResult<TaskRecord>>, IAuthorizedWorkspaceRequest;
