using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Diagnostics;

namespace Api;

public class GetBootstrapHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, SyncQueryService syncQueryService, ILogger<GetBootstrapHandler> logger) : IQueryHandler<GetBootstrapQuery, BootstrapResult>
{
    public async Task<Result<BootstrapResult>> Handle(GetBootstrapQuery request, CancellationToken cancellationToken)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var isOwner = workspaceContext.CurrentMember.Role == Role.Owner;
        var connection = db.Database.GetDbConnection();

        const string visibilityFilter = "(s.is_private = false OR @IsOwner = true)";

        const string tasksSql = @"
            SELECT
                t.id AS Id, t.project_space_id AS SpaceId, t.project_folder_id AS FolderId,
                t.name AS Name, t.custom_color AS Color, t.custom_icon AS Icon,
                t.default_document_id AS DefaultDocumentId,
                t.is_archived AS IsArchived, t.priority AS Priority,
                t.story_points AS StoryPoints, t.time_estimate_seconds AS TimeEstimateSeconds,
                t.status_id AS StatusId,
                t.start_date AS StartDate, t.due_date AS DueDate, t.created_at AS CreatedAt,
                t.parent_task_id AS ParentTaskId
            FROM project_tasks t
            INNER JOIN project_spaces s ON s.id = t.project_space_id AND s.deleted_at IS NULL
            WHERE t.project_workspace_id = @WorkspaceId AND t.deleted_at IS NULL
              AND " + visibilityFilter + ";";

        const string spacesSql = @"
            SELECT
                s.id AS Id, s.project_workspace_id AS WorkspaceId, s.name AS Name,
                s.custom_color AS Color, s.custom_icon AS Icon, s.is_private AS IsPrivate,
                s.order_key AS OrderKey, s.default_document_id AS DefaultDocumentId,
                s.created_at AS CreatedAt, s.creator_id AS CreatorId,
                EXISTS(SELECT 1 FROM project_folders f WHERE f.project_space_id = s.id AND f.deleted_at IS NULL) AS HasFolders,
                EXISTS(SELECT 1 FROM project_tasks t WHERE t.project_space_id = s.id AND t.project_folder_id IS NULL AND t.deleted_at IS NULL) AS HasTasks
            FROM project_spaces s
            WHERE s.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL
              AND " + visibilityFilter + ";";

        const string foldersSql = @"
            SELECT
                f.id AS Id, f.project_workspace_id AS WorkspaceId, f.project_space_id AS SpaceId,
                f.name AS Name, f.created_at AS CreatedAt, f.start_date AS StartDate, f.due_date AS DueDate,
                f.order_key AS OrderKey, f.custom_icon AS Icon, f.custom_color AS Color,
                EXISTS(SELECT 1 FROM project_tasks t WHERE t.project_folder_id = f.id AND t.deleted_at IS NULL) AS HasTasks
            FROM project_folders f
            INNER JOIN project_spaces s ON s.id = f.project_space_id AND s.deleted_at IS NULL
            WHERE f.project_workspace_id = @WorkspaceId AND f.deleted_at IS NULL
              AND " + visibilityFilter + ";";

        // Status is workspace-visible everywhere, not gated by space visibility — every member
        // gets every workspace status regardless of its optional space "ancestor" tag.
        const string statusesSql = @"
            SELECT
                st.id AS Id, st.project_space_id AS SpaceId, st.name AS Name,
                st.color AS Color, st.order_key AS OrderKey
            FROM statuses st
            WHERE st.project_workspace_id = @WorkspaceId AND st.deleted_at IS NULL;";

        const string assigneesSql = @"
            SELECT
                ta.id AS Id, ta.project_task_id AS TaskId, ta.workspace_member_id AS WorkspaceMemberId
            FROM task_assignments ta
            INNER JOIN project_tasks t ON t.id = ta.project_task_id AND t.deleted_at IS NULL
            INNER JOIN project_spaces s ON s.id = t.project_space_id AND s.deleted_at IS NULL
            WHERE t.project_workspace_id = @WorkspaceId AND ta.deleted_at IS NULL
              AND " + visibilityFilter + ";";

        const string favoritesSql = @"
            SELECT
                fav.id AS Id, fav.entity_id AS EntityId, fav.entity_layer_type AS EntityLayerType,
                fav.order_key AS OrderKey
            FROM favorites fav
            WHERE fav.workspace_member_id = @MemberId AND fav.project_workspace_id = @WorkspaceId
              AND fav.deleted_at IS NULL;";

        const string membersSql = @"
            SELECT
                wm.id AS Id, wm.user_id AS UserId, u.name AS Name, u.email AS Email,
                wm.role AS Role, wm.status AS Status, wm.created_at AS CreatedAt, wm.joined_at AS JoinedAt
            FROM workspace_members wm
            INNER JOIN users u ON u.id = wm.user_id AND u.deleted_at IS NULL
            WHERE wm.project_workspace_id = @WorkspaceId AND wm.deleted_at IS NULL;";

        var parameters = new
        {
            WorkspaceId = request.WorkspaceId,
            MemberId = workspaceContext.CurrentMember.Id,
            IsOwner = isOwner
        };

        var combinedSql = string.Join("\n", tasksSql, spacesSql, foldersSql, statusesSql, assigneesSql, favoritesSql, membersSql);

        List<TaskRecord> tasks;
        List<SpaceRecord> spaces;
        List<FolderRecord> folders;
        List<StatusRecord> statuses;
        List<AssigneeRecord> assignees;
        List<FavoriteRecord> favorites;
        List<MemberRecord> members;

        var queryStopwatch = Stopwatch.StartNew();
        await using (var multi = await connection.QueryMultipleAsync(combinedSql, parameters))
        {
            tasks = (await multi.ReadAsync<TaskRecord>()).AsList();
            spaces = (await multi.ReadAsync<SpaceRecord>()).AsList();
            folders = (await multi.ReadAsync<FolderRecord>()).AsList();
            statuses = (await multi.ReadAsync<StatusRecord>()).AsList();
            assignees = (await multi.ReadAsync<AssigneeRecord>()).AsList();
            favorites = (await multi.ReadAsync<FavoriteRecord>()).AsList();
            members = (await multi.ReadAsync<MemberRecord>()).AsList();
        }
        queryStopwatch.Stop();

        var lastSyncId = await syncQueryService.GetLastSyncIdAsync(request.WorkspaceId, cancellationToken);

        totalStopwatch.Stop();
        logger.LogInformation(
            "Bootstrap for workspace {WorkspaceId}: query={QueryMs}ms total={TotalMs}ms (tasks={TaskCount} spaces={SpaceCount} folders={FolderCount} statuses={StatusCount} assignees={AssigneeCount} favorites={FavoriteCount} members={MemberCount})",
            request.WorkspaceId, queryStopwatch.ElapsedMilliseconds, totalStopwatch.ElapsedMilliseconds,
            tasks.Count, spaces.Count, folders.Count, statuses.Count, assignees.Count, favorites.Count, members.Count);

        return Result<BootstrapResult>.Success(new BootstrapResult(lastSyncId, SyncQueryService.CurrentDatabaseVersion, tasks, spaces, folders, statuses, assignees, favorites, members));
    }
}
