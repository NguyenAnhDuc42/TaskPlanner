using Application.Common.Interfaces;

namespace Application.Features.TaskFeatures.Helpers.GetTasksByUser;

public record GetTasksByUserQuery(Guid MemberId) : IQueryRequest<List<TaskSummaryDto>>;

public record TaskSummaryDto(Guid TaskId, string Name);