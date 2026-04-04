namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public static class GetHierarchySql
{
    public const string Query = @"
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
        ),
        user_tasks AS (
            SELECT 
                t.id,
                COALESCE(t.project_folder_id, t.project_space_id) as parent_id,
                t.name,
                t.status_id,
                t.priority,
                t.project_workspace_id,
                t.order_key,
                t.project_folder_id,
                t.project_space_id
            FROM project_tasks t
            WHERE t.project_workspace_id = @WorkspaceId
              AND t.deleted_at IS NULL
              AND t.is_archived = false
        )
        SELECT 
            'Space' as item_type,
            id,
            project_workspace_id::text as parent_id,
            name,
            custom_color as color,
            custom_icon as icon,
            is_private,
            order_key,
            NULL::uuid as status_id,
            0 as priority,
            0 as sort_group
        FROM user_spaces
        UNION ALL
        SELECT 
            'Folder' as item_type,
            id,
            project_space_id::text as parent_id,
            name,
            custom_color as color,
            custom_icon as icon,
            is_private,
            order_key,
            NULL::uuid as status_id,
            0 as priority,
            0 as sort_group
        FROM user_folders
        UNION ALL
        SELECT 
            'Task' as item_type,
            id,
            parent_id::text,
            name,
            '' as color,
            '' as icon,
            false as is_private,
            order_key,
            status_id,
            CAST(priority as integer) as priority,
            1 as sort_group
        FROM user_tasks
        ORDER BY sort_group, order_key;";
}
