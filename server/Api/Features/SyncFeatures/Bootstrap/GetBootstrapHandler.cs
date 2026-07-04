using Application;
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
                ea.access_level AS AccessLevel
            FROM project_tasks t
            INNER JOIN project_spaces s ON s.id = t.project_space_id AND s.deleted_at IS NULL
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id
                AND ea.workspace_member_id = @MemberId
                AND ea.deleted_at IS NULL
            WHERE t.project_workspace_id = @WorkspaceId AND t.deleted_at IS NULL
              AND " + visibilityFilter + ";";

        const string spacesSql = @"
            SELECT
                s.id AS Id, s.project_workspace_id AS WorkspaceId, s.name AS Name,
                s.custom_color AS Color, s.custom_icon AS Icon, s.is_private AS IsPrivate,
                s.order_key AS OrderKey, s.default_document_id AS DefaultDocumentId,
                s.created_at AS CreatedAt, s.creator_id AS CreatorId,
                ea.access_level AS AccessLevel,
                EXISTS(SELECT 1 FROM project_folders f WHERE f.project_space_id = s.id AND f.deleted_at IS NULL) AS HasFolders,
                EXISTS(SELECT 1 FROM project_tasks t WHERE t.project_space_id = s.id AND t.project_folder_id IS NULL AND t.deleted_at IS NULL) AS HasTasks
            FROM project_spaces s
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id
                AND ea.workspace_member_id = @MemberId
                AND ea.deleted_at IS NULL
            WHERE s.project_workspace_id = @WorkspaceId AND s.deleted_at IS NULL
              AND " + visibilityFilter + ";";

        const string foldersSql = @"
            SELECT
                f.id AS Id, f.project_workspace_id AS WorkspaceId, f.project_space_id AS SpaceId,
                f.name AS Name, f.created_at AS CreatedAt, f.start_date AS StartDate, f.due_date AS DueDate,
                f.order_key AS OrderKey, f.custom_icon AS Icon, f.custom_color AS Color,
                ea.access_level AS AccessLevel,
                EXISTS(SELECT 1 FROM project_tasks t WHERE t.project_folder_id = f.id AND t.deleted_at IS NULL) AS HasTasks
            FROM project_folders f
            INNER JOIN project_spaces s ON s.id = f.project_space_id AND s.deleted_at IS NULL
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id
                AND ea.workspace_member_id = @MemberId
                AND ea.deleted_at IS NULL
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

        // DocumentBlock deliberately NOT bootstrapped — same unbounded-content shape as Comment
        // (every block of every Space/Task document, workspace-wide, on every cold start). Moved
        // to the `lazy` load tier: fetched per-document on first open via
        // GET /api/documents/{documentId}/sync/blocks, see FRONTEND_SYNC_CONTEXT.md §1b.

        // TaskAssignment isn't a TenantEntity (no direct workspace_id) — scoped via project_tasks,
        // same join shape as DocumentBlock's document_spaces above. Assignees per task are few
        // (unlike Comment, which stays a lazy per-task fetch), so bulk-loading is cheap.
        const string assigneesSql = @"
            SELECT
                ta.id AS Id, ta.project_task_id AS TaskId, ta.workspace_member_id AS WorkspaceMemberId
            FROM task_assignments ta
            INNER JOIN project_tasks t ON t.id = ta.project_task_id AND t.deleted_at IS NULL
            INNER JOIN project_spaces s ON s.id = t.project_space_id AND s.deleted_at IS NULL
            LEFT JOIN entity_access ea ON ea.project_space_id = s.id
                AND ea.workspace_member_id = @MemberId
                AND ea.deleted_at IS NULL
            WHERE t.project_workspace_id = @WorkspaceId AND ta.deleted_at IS NULL
              AND " + visibilityFilter + ";";

        // Favorites are personal (per member) and never broadcast — a single flat list keyed by
        // entityId, not joined onto Task/Folder/Space. No visibility filter needed beyond "this
        // member's own rows in this workspace": a favorite pointing at an entity the member can no
        // longer see is just a dangling reference the UI quietly ignores when it can't resolve it.
        const string favoritesSql = @"
            SELECT
                fav.id AS Id, fav.entity_id AS EntityId, fav.entity_layer_type AS EntityLayerType,
                fav.order_key AS OrderKey
            FROM favorites fav
            WHERE fav.workspace_member_id = @MemberId AND fav.project_workspace_id = @WorkspaceId
              AND fav.deleted_at IS NULL;";

        // Members are workspace-wide, not space-scoped — every member can see every other member,
        // no entity_access visibility filter needed here (unlike the space-scoped queries above).
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

        // All 8 queries used to run as 8 sequential round-trips to Postgres — with the DB in a
        // different region from most clients, that's 8x the network latency alone, on top of
        // actual query time. QueryMultipleAsync batches every statement into one command and
        // reads the result sets back in order over a single round-trip. Same queries, same data,
        // same visibilityFilter/parameters shared across all of them — just one trip instead of
        // eight. Order here MUST match the order results are read below.
        var combinedSql = string.Join("\n", tasksSql, spacesSql, foldersSql, statusesSql, assigneesSql, favoritesSql, membersSql);

        List<TaskRecord> tasks;
        List<SpaceRecord> spaces;
        List<FolderRecord> folders;
        List<StatusRecord> statuses;
        List<AssigneeRecord> assignees;
        List<FavoriteRecord> favorites;
        List<MemberRecord> members;

        // Timed separately from total handler time so a slow Bootstrap can be attributed to
        // "the round-trip + query execution" vs. "everything else" (permission checks,
        // GetLastSyncIdAsync, serialization) before deciding whether the parallel-connection
        // split (hierarchy group vs. secondary group) is actually worth the added complexity.
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
