using Application.Common.Interfaces;

namespace Application.Features.TaskFeatures.Helpers.GetTasksByUser;

public record GetTasksByUserQuery(Guid workspaceMemberId) : IQuery<List<AssignedTaskDto>>;

public record AssignedTaskDto(Guid TaskId,string Name);