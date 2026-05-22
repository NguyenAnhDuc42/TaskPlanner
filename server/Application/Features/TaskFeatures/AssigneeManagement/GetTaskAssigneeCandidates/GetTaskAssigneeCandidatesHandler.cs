using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Application;

public class GetTaskAssigneeCandidatesHandler(TaskPlanDbContext db)
    : IQueryHandler<GetTaskAssigneeCandidatesQuery, List<TaskAssigneeCandidateDto>>
{
    public async Task<Result<List<TaskAssigneeCandidateDto>>> Handle(GetTaskAssigneeCandidatesQuery request, CancellationToken ct)
    {
        var task = await db.ProjectTasks.FindAsync(request.TaskId, ct);
        if (task == null) return TaskError.NotFound;

        var assignedUserIds = (await db.Database.GetDbConnection().QueryAsync<Guid>(
            GetTaskAssigneeCandidatesSQL.GetAssignedUserIds, 
            new { TaskId = task.Id })).ToArray();

        var safeLimit = request.Limit <= 0 ? 50 : Math.Min(request.Limit, 100);

        var candidates = await db.Database.GetDbConnection().QueryAsync<TaskAssigneeCandidateDto>(
            GetTaskAssigneeCandidatesSQL.GetCandidates, 
            new
            {
                WorkspaceId = task.ProjectWorkspaceId,
                Search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(),
                AssignedUserIds = assignedUserIds,
                Limit = safeLimit
            });

        return candidates.ToList();
    }
}


