using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Dapper;

namespace Application;

public class GetTaskAssigneeCandidatesHandler(TaskPlanDbContext db)
    : IQueryHandler<GetTaskAssigneeCandidatesQuery, List<AssigneeRecord>>
{
    public async Task<Result<List<AssigneeRecord>>> Handle(GetTaskAssigneeCandidatesQuery request, CancellationToken ct)
    {
        var task = await db.ProjectTasks.FindAsync(request.TaskId, ct);
        if (task == null) return TaskError.NotFound;

        const string assignedSql = @"
            SELECT wm.user_id
            FROM task_assignments ta
            JOIN workspace_members wm ON ta.workspace_member_id = wm.id
            WHERE ta.task_id = @TaskId
              AND ta.deleted_at IS NULL
              AND wm.deleted_at IS NULL";

        var connection = db.Database.GetDbConnection();
        var assignedUserIds = (await connection.QueryAsync<Guid>(
            assignedSql, 
            new { TaskId = task.Id })).ToArray();

        var safeLimit = request.Limit <= 0 ? 50 : Math.Min(request.Limit, 100);

        var searchStr = string.IsNullOrWhiteSpace(request.Search) ? (object)DBNull.Value : request.Search.Trim();
        var assignedArray = assignedUserIds.Length > 0 ? assignedUserIds : Array.Empty<Guid>();

        const string candidatesSql = @"
            SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
            FROM workspace_members wm
            JOIN users u ON wm.user_id = u.id
            WHERE wm.project_workspace_id = @WorkspaceId
              AND wm.deleted_at IS NULL
              AND (@Search IS NULL OR u.name ILIKE ('%' || @Search || '%'))
              AND (array_length(@AssignedUserIds, 1) IS NULL OR NOT (u.id = ANY(@AssignedUserIds)))
            ORDER BY u.name
            LIMIT @Limit";

        var candidates = (await connection.QueryAsync<AssigneeRecord>(
            candidatesSql, 
            new { 
                WorkspaceId = task.ProjectWorkspaceId,
                Search = searchStr,
                AssignedUserIds = assignedArray,
                Limit = safeLimit
            }
        )).AsList();

        return candidates;
    }
}


