using MediatR;
using src.Helper.Results;

namespace src.Feature.Workspace.MyWork.AssignedTasks;

public record class AssignedTasksRequest(Guid workspaceId) : IRequest<Result<TaskItems, ErrorResponse>>;
