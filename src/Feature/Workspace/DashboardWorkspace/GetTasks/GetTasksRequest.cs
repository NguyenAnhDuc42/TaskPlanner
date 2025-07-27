using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.Workspace.DashboardWorkspace.GetTasks;

public record class GetTasksRequest(Guid workspaceId) : IRequest<Result<List<TaskSummary>, ErrorResponse>>;
