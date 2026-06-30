using Application;
using Microsoft.EntityFrameworkCore;
using Dapper;

namespace Api;

public class GetBootstrapHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, SyncQueryService syncQueryService) : IQueryHandler<GetBootstrapQuery, BootstrapResult>
{
    public async Task<Result<BootstrapResult>> Handle(GetBootstrapQuery request, CancellationToken cancellationToken)
    {
        var isOwner = workspaceContext.CurrentMember.Role == Role.Owner;
        var connection = db.Database.GetDbConnection();

        const string tasksSql = @"
            SELECT
                t.id AS Id, t.project_space_id AS SpaceId, t.project_folder_id AS FolderId,
                t.name AS Name, t.custom_color AS Color, t.custom_icon AS Icon,
                t.default_document_id AS DefaultDocumentId,
                t.is_archived AS IsArchived, t.priority AS Priority,
                t.story_points AS StoryPoints, t.time_estimate_seconds AS TimeEstimateSeconds,
                t.status_id AS StatusId,
                t.start_date AS StartDate, t.due_date AS DueDate, t.created_at AS CreatedAt,
                t.parent_task_id AS ParentTaskId,
                ea.access_level AS AccessLevel,
                CAST(CASE WHEN fav.id IS NOT NULL THEN 1 ELSE 0 END AS BOOLEAN) AS IsFavorite
            FROM project_tasks t
            INNER JOIN project_spaces s ON s.id = t.project_space_id AND s.deleted_at IS NULL
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id
                AND ea.workspace_member_id = @MemberId
                AND ea.deleted_at IS NULL
            LEFT JOIN favorites fav ON fav.entity_id = t.id AND fav.workspace_member_id = @MemberId
            WHERE t.project_workspace_id = @WorkspaceId AND t.deleted_at IS NULL
              AND (s.is_private = false OR (ea.id IS NOT NULL AND ea.access_level IN ('Viewer', 'Editor', 'Manager')) OR @IsOwner = true);";

        var parameters = new
        {
            WorkspaceId = request.WorkspaceId,
            MemberId = workspaceContext.CurrentMember.Id,
            IsOwner = isOwner
        };

        var tasks = (await connection.QueryAsync<TaskRecord>(tasksSql, parameters)).AsList();
        var lastSyncId = await syncQueryService.GetLastSyncIdAsync(request.WorkspaceId, cancellationToken);

        return Result<BootstrapResult>.Success(new BootstrapResult(lastSyncId, SyncQueryService.CurrentDatabaseVersion, tasks));
    }
}
