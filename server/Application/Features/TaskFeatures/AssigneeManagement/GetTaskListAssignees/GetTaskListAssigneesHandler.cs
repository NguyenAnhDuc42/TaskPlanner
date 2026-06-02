using Microsoft.EntityFrameworkCore;
using Dapper;


namespace Application;

public class GetTaskListAssigneesHandler(TaskPlanDbContext db) : IQueryHandler<GetTaskListAssigneesQuery, List<AssigneeRecord>>
{
    public async Task<Result<List<AssigneeRecord>>> Handle(GetTaskListAssigneesQuery request, CancellationToken cancellationToken)
    {
        var folder = await db.ProjectFolders.FindAsync(request.ListId, cancellationToken);
        if (folder == null) return FolderError.NotFound;

        const string sql = @"
            SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
            FROM workspace_members wm
            JOIN users u ON wm.user_id = u.id
            WHERE wm.project_workspace_id = @WorkspaceId
              AND wm.deleted_at IS NULL
            ORDER BY u.name";

        var connection = db.Database.GetDbConnection();
        var members = (await connection.QueryAsync<AssigneeRecord>(
            sql, 
            new { WorkspaceId = folder.ProjectWorkspaceId })).AsList();

        return members;
    }
}


