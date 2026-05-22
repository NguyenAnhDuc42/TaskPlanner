using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class GetTaskAssigneesHandler(TaskPlanDbContext db) : IQueryHandler<GetTaskAssigneesQuery, List<TaskAssigneeDto>>
{
    public async Task<Result<List<TaskAssigneeDto>>> Handle(GetTaskAssigneesQuery request, CancellationToken ct)
    {
        var task = await db.ProjectTasks.FindAsync(request.TaskId, ct);
        if (task == null) return TaskError.NotFound;

        var assignees = await db.Database.GetDbConnection().QueryAsync<TaskAssigneeDto>(
            GetTaskAssigneesSQL.GetAssignees, 
            new { TaskId = task.Id });

        return assignees.ToList();
    }
}


