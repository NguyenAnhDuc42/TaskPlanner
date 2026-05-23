using Microsoft.EntityFrameworkCore;


namespace Application;

public class GetTaskListAssigneesHandler(TaskPlanDbContext db) : IQueryHandler<GetTaskListAssigneesQuery, List<AssigneeRecord>>
{
    public async Task<Result<List<AssigneeRecord>>> Handle(GetTaskListAssigneesQuery request, CancellationToken ct)
    {
        var folder = await db.ProjectFolders.FindAsync(request.ListId, ct);
        if (folder == null) return FolderError.NotFound;

        const string sql = @"
            SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
            FROM workspace_members wm
            JOIN users u ON wm.user_id = u.id
            WHERE wm.project_workspace_id = @WorkspaceId
              AND wm.deleted_at IS NULL
            ORDER BY u.name";

        var members = await db.Database.SqlQueryRaw<AssigneeRecord>(
            sql, 
            new Npgsql.NpgsqlParameter("WorkspaceId", folder.ProjectWorkspaceId)).ToListAsync(ct);

        return members;
    }
}


