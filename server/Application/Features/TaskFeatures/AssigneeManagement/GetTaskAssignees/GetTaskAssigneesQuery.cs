using Application.Common.Interfaces;

namespace Application.Features.TaskFeatures.AssigneeManagement.GetTaskAssignees;

public record GetTaskAssigneesQuery(Guid TaskId) : IQueryRequest<List<TaskAssigneeDto>>;

public record TaskAssigneeDto(
    Guid UserId,
    string UserName,
    string? AvatarUrl
);
