using Domain.Enums.RelationShip;

namespace Application.Features.ViewFeatures.FeatureHelpers;

/// <summary>
/// Provides raw, access-controlled SQL for fetching tasks per layer type.
/// All queries handle is_private checks via entity_access + workspace_members joins.
/// Column aliases map snake_case DB columns → PascalCase TaskDto properties.
/// </summary>
public static class TaskSql
{
    // Shared column list with aliases for Dapper → TaskDto mapping
    private const string TaskColumns = @"
        t.id                   AS Id,
        t.project_workspace_id AS ProjectWorkspaceId,
        t.project_space_id     AS ProjectSpaceId,
        t.project_folder_id    AS ProjectFolderId,
        t.name                 AS Name,
        t.description          AS Description,
        t.status_id            AS StatusId,
        t.priority             AS Priority,
        t.start_date           AS StartDate,
        t.due_date             AS DueDate,
        t.story_points         AS StoryPoints,
        t.time_estimate        AS TimeEstimate,
        t.order_key            AS OrderKey,
        t.created_at           AS CreatedAt";

    // For ListTasksSql where there's no alias prefix
    private const string TaskColumnsNoAlias = @"
        id                   AS Id,
        project_workspace_id AS ProjectWorkspaceId,
        project_space_id     AS ProjectSpaceId,
        project_folder_id    AS ProjectFolderId,
        name                 AS Name,
        description          AS Description,
        status_id            AS StatusId,
        priority             AS Priority,
        start_date           AS StartDate,
        due_date             AS DueDate,
        story_points         AS StoryPoints,
        time_estimate        AS TimeEstimate,
        order_key            AS OrderKey,
        created_at           AS CreatedAt";

    public static string GetSql(EntityLayerType layerType) => layerType switch
    {
        EntityLayerType.ProjectWorkspace => WorkspaceTasksSql,
        EntityLayerType.ProjectSpace     => SpaceTasksSql,
        EntityLayerType.ProjectFolder    => FolderTasksSql,
        _ => throw new NotSupportedException($"LayerType {layerType} is not supported for Task views.")
    };

    private static readonly string WorkspaceTasksSql = $@"
        SELECT DISTINCT {TaskColumns}
        FROM project_tasks t
        LEFT JOIN project_spaces s  ON t.project_space_id  = s.id
        LEFT JOIN project_folders f ON t.project_folder_id = f.id
        -- Space access
        LEFT JOIN entity_access ea_s      ON ea_s.entity_id = s.id
                                         AND ea_s.entity_layer = 'ProjectSpace'
                                         AND ea_s.deleted_at IS NULL
        LEFT JOIN workspace_members wm_s  ON wm_s.id = ea_s.workspace_member_id
                                         AND wm_s.id = @WorkspaceMemberId
                                         AND wm_s.deleted_at IS NULL
        -- Folder access
        LEFT JOIN entity_access ea_f      ON ea_f.entity_id = f.id
                                         AND ea_f.entity_layer = 'ProjectFolder'
                                         AND ea_f.deleted_at IS NULL
        LEFT JOIN workspace_members wm_f  ON wm_f.id = ea_f.workspace_member_id
                                         AND wm_f.id = @WorkspaceMemberId
                                         AND wm_f.deleted_at IS NULL
        WHERE t.project_workspace_id = @layerId
          AND t.is_archived = false
          AND t.deleted_at IS NULL
          AND (s.id IS NULL OR s.is_private = false OR wm_s.id IS NOT NULL)
          AND (f.id IS NULL OR f.is_private = false OR wm_f.id IS NOT NULL)
        ORDER BY t.created_at
    ";

    private static readonly string SpaceTasksSql = $@"
        SELECT DISTINCT {TaskColumns}
        FROM project_tasks t
        LEFT JOIN project_folders f ON t.project_folder_id = f.id
        -- Folder access
        LEFT JOIN entity_access ea_f      ON ea_f.entity_id = f.id
                                         AND ea_f.entity_layer = 'ProjectFolder'
                                         AND ea_f.deleted_at IS NULL
        LEFT JOIN workspace_members wm_f  ON wm_f.id = ea_f.workspace_member_id
                                         AND wm_f.id = @WorkspaceMemberId
                                         AND wm_f.deleted_at IS NULL
        WHERE t.project_space_id = @layerId
          AND t.is_archived = false
          AND t.deleted_at IS NULL
          AND (f.id IS NULL OR f.is_private = false OR wm_f.id IS NOT NULL)
        ORDER BY t.created_at
    ";

    private static readonly string FolderTasksSql = $@"
        SELECT DISTINCT {TaskColumns}
        FROM project_tasks t
        WHERE t.project_folder_id = @layerId
          AND t.is_archived = false
          AND t.deleted_at IS NULL
        ORDER BY t.created_at
    ";


}
