using System;
using System.Data;
using Dapper;
using MediatR;
using src.Contract;
using src.Helper.Results;
using src.Infrastructure.Abstractions.IRepositories;
using src.Infrastructure.Abstractions.IServices;

namespace src.Feature.WorkspaceManager.MyWork.AssignedTasks;

public class AssignedTasksHandler : IRequestHandler<AssignedTasksRequest, Result<List<TaskSummary>, ErrorResponse>>
{
    private readonly IDbConnection _dbConnection;
    private readonly ICurrentUserService _currentUserService;
    private readonly IHierarchyRepository _hierarchyRepository;
    public AssignedTasksHandler(IDbConnection dbConnection, ICurrentUserService currentUserService, IHierarchyRepository hierarchyRepository)
    {
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
    }

    public async Task<Result<List<TaskSummary>, ErrorResponse>> Handle(AssignedTasksRequest request, CancellationToken cancellationToken)
    {
        var workspace = await _hierarchyRepository.GetWorkspaceByIdAsync(request.workspaceId, cancellationToken);
        if (workspace == null)
        {
            return Result<List<TaskSummary>, ErrorResponse>.Failure(ErrorResponse.NotFound("Workspace not found"));
        }
        var userId = _currentUserService.CurrentUserId();
        const string sql = @"SELECT t.""Id"", t.""Name"", t.""DueDate"", t.""Status"", t.""Priority"",
                                    u.""Id"", u.""Name"", u.""Email""
                            FROM ""PlanTasks"" AS t
                            LEFT JOIN ""UserTasks"" AS ut ON t.""Id"" = ut.""TaskId""
                            LEFT JOIN ""Users"" AS u ON ut.""UserId"" = u.""Id""
                            WHERE t.""WorkspaceId"" = @WorkspaceId
                            AND EXISTS(
                                    SELECT 1
                                    FROM ""UserTasks"" AS ut_filter
                                    WHERE ut_filter.""TaskId"" = t.""Id""
                                    AND ut_filter.""UserId"" = @UserId
                            )
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
                if (assignee != null && assignee.Id != Guid.Empty && !taskEntry.assignees.Any(a => a.Id == assignee.Id))
                {
                    taskEntry.assignees.Add(assignee);
                }
                return taskEntry;
            },
            new { WorkspaceId = request.workspaceId, UserId = userId },
            splitOn: "Id");
            
        return Result<List<TaskSummary>, ErrorResponse>.Success(taskDictionary.Values.ToList());
    }
}
