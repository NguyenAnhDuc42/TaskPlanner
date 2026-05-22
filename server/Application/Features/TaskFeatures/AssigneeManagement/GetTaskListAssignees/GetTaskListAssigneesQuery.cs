namespace Application;

public record GetTaskListAssigneesQuery(Guid ListId) : IQueryRequest<List<TaskAssigneeOptionDto>>;

public record TaskAssigneeOptionDto(
    Guid UserId,
    string UserName,
    string? AvatarUrl
);


