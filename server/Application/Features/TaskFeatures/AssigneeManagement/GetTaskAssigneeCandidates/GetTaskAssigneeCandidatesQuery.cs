using Application.Common.Interfaces;

namespace Application.Features.TaskFeatures.AssigneeManagement.GetTaskAssigneeCandidates;

public record GetTaskAssigneeCandidatesQuery(
    Guid TaskId,
    string? Search = null,
    int Limit = 50) : IQueryRequest<List<TaskAssigneeCandidateDto>>;

public record TaskAssigneeCandidateDto(
    Guid UserId,
    string UserName,
    string? AvatarUrl
);
