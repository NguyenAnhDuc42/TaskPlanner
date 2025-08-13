using MediatR;
using src.Contract;
using src.Domain.Enums;
using src.Helper.Filters;
using src.Helper.Results;

namespace src.Feature.TaskManager.GetTasks;

public record class GetTasksRequest(TaskQuery query) : IRequest<PagedResult<TasksSummary>>;


