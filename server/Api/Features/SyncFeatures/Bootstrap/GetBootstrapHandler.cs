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

        // Shared visibility rule across every space-scoped query below:
        // a private space is visible only if the caller has an explicit
        // entity_access grant on it (or is the workspace Owner).
        const string visibilityFilter = "(s.is_private = false OR (ea.id IS NOT NULL AND ea.access_level IN ('Viewer', 'Editor', 'Manager')) OR @IsOwner = true)";

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
              AND " + visibilityFilter + ";";

        const string spacesSql = @"
            SELECT
                s.id AS Id, s.project_workspace_id AS WorkspaceId, s.name AS Name,
                s.custom_color AS Color, s.custom_icon AS Icon, s.is_private AS IsPrivate,
                s.order_key AS OrderKey, s.default_document_id AS DefaultDocumentId,
                s.created_at AS CreatedAt, s.creator_id AS CreatorId,
                ea.access_level AS AccessLevel,
                CAST(CASE WHEN fav.id IS NOT NULL THEN 1 ELSE 0 END AS BOOLEAN) AS IsFavorite,
                EXISTS(SELECT 1 FROM project_folders f WHERE f.project_space_id = s.id AND f.deleted_at IS NULL) AS HasFolders,
                EXISTS(SELECT 1 FROM project_tasks t WHERE t.project_space_id = s.id AND t.project_folder_id IS NULL AND t.deleted_at IS NULL) AS HasTasks
            FROM project_spaces s
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id
                AND ea.workspace_member_id = @MemberId
                AND ea.deleted_at IS NULL
            LEFT JOIN favorites fav ON fav.entity_id = s.id AND fav.workspace_member_id = @MemberId
            WHERE s.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL
              AND " + visibilityFilter + ";";

        const string foldersSql = @"
            SELECT
                f.id AS Id, f.project_workspace_id AS WorkspaceId, f.project_space_id AS SpaceId,
                f.name AS Name, f.created_at AS CreatedAt, f.start_date AS StartDate, f.due_date AS DueDate,
                f.order_key AS OrderKey, f.custom_icon AS Icon, f.custom_color AS Color,
                ea.access_level AS AccessLevel,
                CAST(CASE WHEN fav.id IS NOT NULL THEN 1 ELSE 0 END AS BOOLEAN) AS IsFavorite,
                EXISTS(SELECT 1 FROM project_tasks t WHERE t.project_folder_id = f.id AND t.deleted_at IS NULL) AS HasTasks
            FROM project_folders f
            INNER JOIN project_spaces s ON s.id = f.project_space_id AND s.deleted_at IS NULL
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id
                AND ea.workspace_member_id = @MemberId
                AND ea.deleted_at IS NULL
            LEFT JOIN favorites fav ON fav.entity_id = f.id AND fav.workspace_member_id = @MemberId
            WHERE f.project_workspace_id = @WorkspaceId AND f.deleted_at IS NULL
              AND " + visibilityFilter + ";";

        const string statusesSql = @"
            SELECT
                st.id AS Id, st.project_space_id AS SpaceId, st.name AS Name,
                st.color AS Color, st.category AS Category, st.order_key AS OrderKey
            FROM statuses st
            INNER JOIN project_spaces s ON s.id = st.project_space_id AND s.deleted_at IS NULL
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id
                AND ea.workspace_member_id = @MemberId
                AND ea.deleted_at IS NULL
            WHERE st.project_workspace_id = @WorkspaceId AND st.deleted_at IS NULL
              AND " + visibilityFilter + ";";

        var parameters = new
        {
            WorkspaceId = request.WorkspaceId,
            MemberId = workspaceContext.CurrentMember.Id,
            IsOwner = isOwner
        };

        var tasks = (await connection.QueryAsync<TaskRecord>(tasksSql, parameters)).AsList();
        var spaces = (await connection.QueryAsync<SpaceRecord>(spacesSql, parameters)).AsList();
        var folders = (await connection.QueryAsync<FolderRecord>(foldersSql, parameters)).AsList();
        var statuses = (await connection.QueryAsync<StatusRecord>(statusesSql, parameters)).AsList();
        var lastSyncId = await syncQueryService.GetLastSyncIdAsync(request.WorkspaceId, cancellationToken);

        return Result<BootstrapResult>.Success(new BootstrapResult(lastSyncId, SyncQueryService.CurrentDatabaseVersion, tasks, spaces, folders, statuses));
    }
}
