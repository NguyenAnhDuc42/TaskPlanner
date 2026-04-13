using Application.Common.Interfaces;

namespace Application.Features.TaskFeatures.Helpers.GetTasksByDay;

public record GetTasksByDayQuery(DateTime day) : IQueryRequest<List<WorkspaceGroupDto>>;

public record WorkspaceGroupDto(Guid WorkspaceId, string Name, List<TaskSummaryDto> Tasks);
public record TaskSummaryDto(Guid TaskId, string Name);