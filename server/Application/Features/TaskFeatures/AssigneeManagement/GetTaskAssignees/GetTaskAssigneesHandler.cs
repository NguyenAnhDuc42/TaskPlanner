using Application.Helpers;
using Application.Common.Errors;
using Application.Common.Results;
using Domain.Entities.ProjectEntities;
using Application.Interfaces;
using Application.Interfaces.Data;
using Dapper;

namespace Application.Features.TaskFeatures;

public class GetTaskAssigneesHandler(IDataBase db) : IQueryHandler<GetTaskAssigneesQuery, List<TaskAssigneeDto>>
{
    public async Task<Result<List<TaskAssigneeDto>>> Handle(GetTaskAssigneesQuery request, CancellationToken ct)
    {
        var task = await db.Tasks.FindAsync(request.TaskId, ct);
        if (task == null) return TaskError.NotFound;

        var assignees = await db.Connection.QueryAsync<TaskAssigneeDto>(
            GetTaskAssigneesSQL.GetAssignees, 
            new { TaskId = task.Id });

        return assignees.ToList();
    }
}
