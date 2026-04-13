namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public static class GetHierarchySql
{
    /// <summary>
    /// Query 1: Structure only — Spaces and Folders.
    /// Run once on workspace open. No tasks. Fast and cacheable.
    /// </summary>
    public const string StructureQuery = @"
        WITH user_spaces AS (
            SELECT 
                s.id,
                s.project_workspace_id,
                s.name,
                s.custom_color,
                s.custom_icon,
                s.is_private,
                s.order_key
            FROM project_spaces s
            WHERE s.project_workspace_id = @WorkspaceId
              AND s.deleted_at IS NULL
              AND s.is_archived = false
              AND (
                  s.is_private = false 
                  OR EXISTS (
                      SELECT 1 FROM entity_access ea
                      INNER JOIN workspace_members wm ON ea.workspace_member_id = wm.id
                      WHERE ea.entity_id = s.id 
                        AND wm.user_id = @UserId 
                        AND wm.project_workspace_id = @WorkspaceId
                        AND ea.entity_layer = 'ProjectSpace'
                        AND ea.deleted_at IS NULL
                        AND wm.deleted_at IS NULL
                  )
              )
        ),
        user_folders AS (
            SELECT 
                f.id,
                f.project_space_id,
                f.name,
                f.custom_color,
                f.custom_icon,
                f.is_private,
                f.order_key
            FROM project_folders f
            INNER JOIN user_spaces us ON f.project_space_id = us.id
            WHERE f.deleted_at IS NULL
              AND f.is_archived = false
              AND (
                  f.is_private = false 
                  OR EXISTS (
                      SELECT 1 FROM entity_access ea
                      INNER JOIN workspace_members wm ON ea.workspace_member_id = wm.id
                      WHERE ea.entity_id = f.id 
                        AND wm.user_id = @UserId 
                        AND wm.project_workspace_id = @WorkspaceId
                        AND ea.entity_layer = 'ProjectFolder'
                        AND ea.deleted_at IS NULL
                        AND wm.deleted_at IS NULL
                  )
              )
        )
        SELECT 
            'Space' as item_type,
            id,
            project_workspace_id as parent_id,
            name,
            custom_color as color,
            custom_icon as icon,
            is_private,
            order_key
        FROM user_spaces

        UNION ALL

        SELECT 
            'Folder' as item_type,
            id,
            project_space_id as parent_id,
            name,
            custom_color as color,
            custom_icon as icon,
            is_private,
            order_key
        FROM user_folders

        ORDER BY order_key, id;";

    /// <summary>
    /// Query 2: Tasks per parent — paginated with a composite cursor (order_key, id).
    /// Called on node expand. Uses partial indexes for performance.
    /// </summary>
    public const string TasksQuery = @"
        SELECT 
            t.id,
            t.name,
            t.status_id,
            t.priority,
            t.order_key,
            t.project_folder_id,
            t.project_space_id,
            CASE 
                WHEN t.project_folder_id IS NOT NULL THEN 'Folder'
                ELSE 'Space'
            END as parent_type
        FROM project_tasks t
        WHERE t.project_workspace_id = @WorkspaceId
          AND t.deleted_at IS NULL
          AND t.is_archived = false
          AND (
              (@ParentType = 'Folder' AND t.project_folder_id = @ParentId)
              OR
              (@ParentType = 'Space' AND t.project_space_id = @ParentId AND t.project_folder_id IS NULL)
          )
          AND (
              @CursorOrderKey IS NULL
              OR (t.order_key, t.id::text) > (@CursorOrderKey, @CursorTaskId)
          )
        ORDER BY t.order_key, t.id
        LIMIT @PageSize;";
}
