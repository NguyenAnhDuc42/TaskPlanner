using Application.Helpers;
using Application.Common.Errors;
using Application.Common.Results;
using Domain.Entities.ProjectEntities;
using server.Application.Interfaces;
using Application.Interfaces.Data;
using Dapper;

namespace Application.Features.TaskFeatures.AssigneeManagement.GetTaskAssignees;

public class GetTaskAssigneesHandler : IQueryHandler<GetTaskAssigneesQuery, List<TaskAssigneeDto>>
{
    private readonly IDataBase _db;

    public GetTaskAssigneesHandler(IDataBase db)
    {
        _db = db;
    }

    public async Task<Result<List<TaskAssigneeDto>>> Handle(GetTaskAssigneesQuery request, CancellationToken ct)
    {
        var task = await _db.Tasks.FindAsync(request.TaskId, ct);
        if (task == null) return TaskError.NotFound;

        var assignees = await _db.Connection.QueryAsync<TaskAssigneeDto>(@"
            SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
            FROM task_assignments ta
            JOIN workspace_members wm ON ta.workspace_member_id = wm.id
            JOIN users u ON wm.user_id = u.id
            WHERE ta.task_id = @TaskId
              AND ta.deleted_at IS NULL
              AND wm.deleted_at IS NULL
            ORDER BY u.name", new { TaskId = task.Id });

        return assignees.ToList();
    }
}
