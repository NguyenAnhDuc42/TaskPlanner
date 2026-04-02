using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.TaskFeatures.Helpers.GetTasksByDay;

public record GetTaskByDayQuery(DateTime day) : IQuery<List<WorkspaceGroupDto>>;

public record WorkspaceGroupDto(Guid WorkspaceId ,string Name , List<TaskSummaryDto> Tasks);
public record TaskSummaryDto(Guid TaskId,string Name);