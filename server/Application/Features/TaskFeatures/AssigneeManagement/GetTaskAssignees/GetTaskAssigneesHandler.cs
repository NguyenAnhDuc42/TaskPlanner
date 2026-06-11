using Microsoft.EntityFrameworkCore;
using Dapper;


namespace Application;

public class GetTaskAssigneesHandler(TaskPlanDbContext db) : IQueryHandler<GetTaskAssigneesQuery, List<AssigneeRecord>>
{
    public async Task<Result<List<AssigneeRecord>>> Handle(GetTaskAssigneesQuery request, CancellationToken cancellationToken)
    {
        var task = await db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId && t.DeletedAt == null, cancellationToken);
        if (task == null) return TaskError.NotFound;

        const string sql = @"
            SELECT ta.id AS Id, ta.project_task_id AS TaskId, ta.workspace_member_id AS WorkspaceMemberId
            FROM task_assignments ta
            WHERE ta.project_task_id = @TaskId
              AND ta.deleted_at IS NULL";

        var connection = db.Database.GetDbConnection();
        var assignees = (await connection.QueryAsync<AssigneeRecord>(
            sql, 
            new { TaskId = task.Id })).AsList();

        return assignees;
    }
}


