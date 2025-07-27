using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.Workspace.MyWork.AssignedTasks;

public record class AssignedTasksRequest(Guid workspaceId) : IRequest<Result<List<TaskSummary>, ErrorResponse>>;
