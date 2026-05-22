namespace Application;

public record GetTaskAssigneeCandidatesQuery(
    Guid TaskId,
    string? Search = null,
    int Limit = 50) : IQueryRequest<List<TaskAssigneeCandidateDto>>;

public record TaskAssigneeCandidateDto(
    Guid UserId,
    string UserName,
    string? AvatarUrl
);


