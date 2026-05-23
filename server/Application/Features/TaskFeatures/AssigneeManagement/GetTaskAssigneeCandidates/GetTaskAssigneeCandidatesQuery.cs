namespace Application;

public record GetTaskAssigneeCandidatesQuery(
    Guid TaskId,
    string? Search = null,
    int Limit = 50) : IQueryRequest<List<AssigneeRecord>>;


