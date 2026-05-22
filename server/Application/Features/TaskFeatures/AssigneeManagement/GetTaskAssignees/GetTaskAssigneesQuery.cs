namespace Application;

public record GetTaskAssigneesQuery(Guid TaskId) : IQueryRequest<List<TaskAssigneeDto>>;

public record TaskAssigneeDto(
    Guid UserId,
    string UserName,
    string? AvatarUrl
);


