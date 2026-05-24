using Microsoft.EntityFrameworkCore;
using Dapper;


namespace Application;

public class GetTaskAssigneesHandler(TaskPlanDbContext db) : IQueryHandler<GetTaskAssigneesQuery, List<AssigneeRecord>>
{
    public async Task<Result<List<AssigneeRecord>>> Handle(GetTaskAssigneesQuery request, CancellationToken ct)
    {
        var task = await db.ProjectTasks.FindAsync(request.TaskId, ct);
        if (task == null) return TaskError.NotFound;

        const string sql = @"
            SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
            FROM task_assignments ta
            JOIN workspace_members wm ON ta.workspace_member_id = wm.id
            JOIN users u ON wm.user_id = u.id
            WHERE ta.task_id = @TaskId
              AND ta.deleted_at IS NULL
              AND wm.deleted_at IS NULL
            ORDER BY u.name";

        var connection = db.Database.GetDbConnection();
        var assignees = (await connection.QueryAsync<AssigneeRecord>(
            sql, 
            new { TaskId = task.Id })).AsList();

        return assignees;
    }
}


