using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using src.Contract;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;

namespace src.Feature.WorkspaceManager.DashboardWorkspace.GetTasks;

public class GetTasksHandler : IRequestHandler<GetTasksRequest, Result<List<TaskSummary>, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;
    private readonly IHierarchyRepository _hierarchyRepository;
    public GetTasksHandler(IDbConnection dbConnection, IHierarchyRepository hierarchyRepository)
    {
        _hierarchyRepository = hierarchyRepository;
        _dbConnection = dbConnection;
    }
    public async Task<Result<List<TaskSummary>, ErrorResponse>> Handle(GetTasksRequest request, CancellationToken cancellationToken)
    {
        var workspace = await _hierarchyRepository.GetWorkspaceByIdAsync(request.workspaceId, cancellationToken);
        if (workspace == null)
        {
            return Result<List<TaskSummary>, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found"));
        }
        const string sql = @"SELECT t.""Id"", t.""Name"", t.""DueDate"", t.""Status"", t.""Priority"",
                                    u.""Id"", u.""Name"", u.""Email""
                            FROM ""PlanTasks"" AS t
                            LEFT JOIN ""UserTasks"" AS ut ON t.""Id"" = ut.""TaskId""
                            LEFT JOIN ""Users"" AS u ON ut.""UserId"" = u.""Id""
                            WHERE t.""WorkspaceId"" = @WorkspaceId
                            ORDER BY t.""CreatedAt"" DESC, t.""Id"";";

        var taskDictionary = new Dictionary<Guid, TaskSummary>();

        await _dbConnection.QueryAsync<TaskSummary, UserSummary, TaskSummary>(
            sql,
            (task, assignee) =>
            {
                if (!taskDictionary.TryGetValue(task.Id, out var taskEntry))
                {
                    taskEntry = task with { assignees = new List<UserSummary>() };
                    taskDictionary.Add(taskEntry.Id, taskEntry);
                }

                if (assignee != null && assignee.Id != Guid.Empty)
                {
                    if (!taskEntry.assignees.Any(a => a.Id == assignee.Id))
                    {
                        taskEntry.assignees.Add(assignee);
                    }
                }
                return taskEntry;
            },
            new { WorkspaceId = request.workspaceId },
            splitOn: "Id");

        return Result<List<TaskSummary>, ErrorResponse>.Success(taskDictionary.Values.ToList());
    }
}
