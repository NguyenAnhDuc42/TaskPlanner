using MediatR;
using src.Contract;
using src.Helper.Results;

namespace src.Feature.WorkspaceManager.DashboardWorkspace.GetTasks;

public record class GetTasksRequest(Guid workspaceId) : IRequest<Result<List<TaskSummary>, ErrorResponse>>;
