namespace Application;

public record GetTaskAssigneesQuery(Guid TaskId) : IQueryRequest<List<AssigneeRecord>>;


