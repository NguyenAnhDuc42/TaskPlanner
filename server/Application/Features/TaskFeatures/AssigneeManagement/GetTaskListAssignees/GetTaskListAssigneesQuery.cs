namespace Application;

public record GetTaskListAssigneesQuery(Guid ListId) : IQueryRequest<List<AssigneeRecord>>;


