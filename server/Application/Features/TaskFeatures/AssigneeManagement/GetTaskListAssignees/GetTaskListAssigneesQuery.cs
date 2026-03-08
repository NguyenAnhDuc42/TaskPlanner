using Application.Common.Interfaces;

namespace Application.Features.TaskFeatures.AssigneeManagement.GetTaskListAssignees;

public record GetTaskListAssigneesQuery(Guid ListId) : IQuery<List<TaskAssigneeOptionDto>>;

public record TaskAssigneeOptionDto(
    Guid UserId,
    string UserName,
    string? AvatarUrl
);
