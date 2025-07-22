using MediatR;
using src.Helper.Results;

namespace src.Feature.Workspace.DashboardWorkspace.GetTasks;

public record class GetTasksRequest(Guid workspaceId) : IRequest<Result<TaskItems, ErrorResponse>>;
