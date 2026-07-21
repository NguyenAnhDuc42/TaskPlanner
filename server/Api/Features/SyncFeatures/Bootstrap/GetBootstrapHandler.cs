using Microsoft.EntityFrameworkCore;
using Dapper;
using System.Diagnostics;

namespace Api;

public class GetBootstrapHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, SyncQueryService syncQueryService, ILogger<GetBootstrapHandler> logger) : IQueryHandler<GetBootstrapQuery, BootstrapResult>
{
    private const int TaskPageSize = 2000;

    public async Task<Result<BootstrapResult>> Handle(GetBootstrapQuery request, CancellationToken cancellationToken)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var isOwner = workspaceContext.CurrentMember.Role == Role.Owner;
        var connection = db.Database.GetDbConnection();
        var isFirstPage = request.AfterCreatedAt is null;

        const string visibilityFilter = "(s.is_private = false OR @IsOwner = true)";

        const string cursorFilter = "(@AfterCreatedAt::timestamptz IS NULL OR (t.created_at, t.id) > (@AfterCreatedAt, @AfterTaskId))";

        var tasksSql = @"
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
              AND " + visibilityFilter + @"
              AND " + cursorFilter + @"
            ORDER BY t.created_at, t.id
            LIMIT " + (TaskPageSize + 1) + ";";

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

        // Scoped to this page's own task ids (not the whole workspace) — assignees ride along with
        // whichever tasks were actually fetched, so a task's assignments never arrive on a
        // different page than the task itself. Task ids here are already visibility-filtered by
        // the tasks query above, so no separate space join/visibility check is needed here.
        const string assigneesSql = @"
            SELECT
                ta.id AS Id, ta.project_task_id AS TaskId, ta.workspace_member_id AS WorkspaceMemberId
            FROM task_assignments ta
            WHERE ta.project_task_id = ANY(@TaskIds) AND ta.deleted_at IS NULL;";

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

        // LastEditedAt: the document row's own updated_at only reflects metadata edits
        // (rename/move/icon) — actual page content lives in document_blocks — so this takes
        // whichever is more recent, the document's own timestamp or the newest of its blocks'.
        // The correlated subquery is backed by the DocumentId index added alongside this feature.
        const string documentsSql = @"
            SELECT
                d.id AS Id, d.project_workspace_id AS WorkspaceId, d.project_space_id AS SpaceId,
                d.parent_document_id AS ParentDocumentId, d.name AS Name, d.order_key AS OrderKey,
                d.icon AS Icon, d.color AS Color, d.created_at AS CreatedAt,
                GREATEST(d.updated_at, COALESCE(
                    (SELECT MAX(db.updated_at) FROM document_blocks db WHERE db.document_id = d.id AND db.deleted_at IS NULL),
                    d.updated_at
                )) AS LastEditedAt
            FROM documents d
            INNER JOIN project_spaces s ON s.id = d.project_space_id AND s.deleted_at IS NULL
            WHERE d.project_workspace_id = @WorkspaceId AND d.deleted_at IS NULL
              AND " + visibilityFilter + ";";

        var parameters = new
        {
            WorkspaceId = request.WorkspaceId,
            MemberId = workspaceContext.CurrentMember.Id,
            IsOwner = isOwner,
            AfterCreatedAt = request.AfterCreatedAt,
            AfterTaskId = request.AfterTaskId,
        };

        // Non-task entities are small and workspace-bounded (not ever-growing like tasks), so they
        // only need fetching once — page 1 returns them all, later pages carry tasks(+assignees) only.
        var combinedSql = isFirstPage
            ? string.Join("\n", tasksSql, spacesSql, foldersSql, statusesSql, favoritesSql, membersSql, documentsSql)
            : tasksSql;

        List<TaskRecord> tasks;
        List<SpaceRecord> spaces = [];
        List<FolderRecord> folders = [];
        List<StatusRecord> statuses = [];
        List<FavoriteRecord> favorites = [];
        List<MemberRecord> members = [];
        List<DocumentRecord> documents = [];

        var queryStopwatch = Stopwatch.StartNew();
        await using (var multi = await connection.QueryMultipleAsync(combinedSql, parameters))
        {
            tasks = (await multi.ReadAsync<TaskRecord>()).AsList();
            if (isFirstPage)
            {
                spaces = (await multi.ReadAsync<SpaceRecord>()).AsList();
                folders = (await multi.ReadAsync<FolderRecord>()).AsList();
                statuses = (await multi.ReadAsync<StatusRecord>()).AsList();
                favorites = (await multi.ReadAsync<FavoriteRecord>()).AsList();
                members = (await multi.ReadAsync<MemberRecord>()).AsList();
                documents = (await multi.ReadAsync<DocumentRecord>()).AsList();
            }
        }

        // Fetched one extra row (TaskPageSize + 1) purely to detect "is there another page" without
        // a separate COUNT query — trim it back off before it's ever exposed to the caller.
        var hasMoreTasks = tasks.Count > TaskPageSize;
        if (hasMoreTasks) tasks.RemoveAt(tasks.Count - 1);
        var nextCursorCreatedAt = hasMoreTasks ? tasks[^1].CreatedAt : (DateTimeOffset?)null;
        var nextCursorId = hasMoreTasks ? tasks[^1].Id : (Guid?)null;

        var assignees = tasks.Count > 0
            ? (await connection.QueryAsync<AssigneeRecord>(assigneesSql, new { TaskIds = tasks.Select(t => t.Id).ToArray() })).AsList()
            : [];
        queryStopwatch.Stop();

        var lastSyncId = await syncQueryService.GetLastSyncIdAsync(request.WorkspaceId, cancellationToken);

        totalStopwatch.Stop();
        logger.LogInformation(
            "Bootstrap for workspace {WorkspaceId} (page after={AfterCreatedAt}): query={QueryMs}ms total={TotalMs}ms (tasks={TaskCount} hasMore={HasMoreTasks} spaces={SpaceCount} folders={FolderCount} statuses={StatusCount} assignees={AssigneeCount} favorites={FavoriteCount} members={MemberCount} documents={DocumentCount})",
            request.WorkspaceId, request.AfterCreatedAt, queryStopwatch.ElapsedMilliseconds, totalStopwatch.ElapsedMilliseconds,
            tasks.Count, hasMoreTasks, spaces.Count, folders.Count, statuses.Count, assignees.Count, favorites.Count, members.Count, documents.Count);

        return Result<BootstrapResult>.Success(new BootstrapResult(
            lastSyncId, SyncQueryService.CurrentDatabaseVersion, tasks, spaces, folders, statuses, assignees, favorites, members, documents,
            hasMoreTasks, nextCursorCreatedAt, nextCursorId));
    }
}
