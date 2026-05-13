namespace Application.Features.WorkspaceFeatures;

public static class GetHierarchySql
{
    public const string GetSpacesAndWorkspaceQuery = @"
        SELECT 
            w.name         AS workspace_name,
            s.id,
            s.name,
            s.custom_color AS color,
            s.custom_icon  AS icon,
            s.is_private,
            s.order_key,
            EXISTS (
                SELECT 1 FROM project_folders f
                WHERE f.project_space_id = s.id
                  AND f.deleted_at IS NULL
                  AND f.is_archived = false
                LIMIT 1
            ) AS has_folders,
            EXISTS (
                SELECT 1 FROM project_tasks t
                WHERE t.project_space_id = s.id
                  AND t.project_folder_id IS NULL
                  AND t.deleted_at IS NULL
                  AND t.is_archived = false
                LIMIT 1
            ) AS has_tasks
        FROM project_workspaces w
        LEFT JOIN project_spaces s
            ON s.project_workspace_id = w.id
           AND s.deleted_at IS NULL
           AND s.is_archived = false
        WHERE w.id = @WorkspaceId
          AND w.deleted_at IS NULL
        ORDER BY s.order_key, s.id;";

    public const string GetFoldersByWorkspaceQuery = @"
        SELECT 
            f.id,
            f.project_space_id AS space_id,
            f.name,
            f.custom_color AS color,
            f.custom_icon  AS icon,
            f.is_private,
            f.order_key,
            EXISTS (
                SELECT 1 FROM project_tasks t
                WHERE t.project_folder_id = f.id
                  AND t.deleted_at IS NULL
                  AND t.is_archived = false
                LIMIT 1
            ) AS has_tasks
        FROM project_folders f
        WHERE f.project_space_id = ANY(@SpaceIds)
          AND f.deleted_at IS NULL
          AND f.is_archived = false
        ORDER BY f.project_space_id, f.order_key, f.id;";

    public const string GetFoldersBySpaceQuery = @"
        SELECT 
            f.id,
            f.project_space_id AS parent_id,
            f.name,
            f.custom_color AS color,
            f.custom_icon  AS icon,
            f.is_private,
            f.order_key,
            EXISTS (
                SELECT 1 FROM project_tasks t
                WHERE t.project_folder_id = f.id
                  AND t.deleted_at IS NULL
                  AND t.is_archived = false
                LIMIT 1
            ) AS has_tasks
        FROM project_folders f
        WHERE f.project_space_id = @SpaceId
          AND f.deleted_at IS NULL
          AND f.is_archived = false
        ORDER BY f.order_key, f.id;";

    public const string TasksQuery = @"
        SELECT 
            t.id,
            t.name,
            t.status_id,
            t.priority,
            t.order_key,
            t.project_folder_id,
            t.project_space_id,
            t.custom_color AS color,
            t.custom_icon AS icon,
            CASE 
                WHEN t.project_folder_id IS NOT NULL THEN 'ProjectFolder'
                ELSE 'ProjectSpace'
            END AS parent_type
        FROM project_tasks t
        WHERE t.project_workspace_id = @WorkspaceId
          AND t.deleted_at IS NULL
          AND t.is_archived = false
          AND (
              (@ParentType = 'ProjectFolder' AND t.project_folder_id = @ParentId)
              OR
              (@ParentType = 'ProjectSpace' AND t.project_space_id = @ParentId AND t.project_folder_id IS NULL)
          )
          AND (
              @CursorOrderKey IS NULL
              OR t.order_key > @CursorOrderKey
              OR (t.order_key = @CursorOrderKey AND t.id > @CursorTaskId::uuid)
          )
        ORDER BY t.order_key, t.id
        LIMIT @PageSize;";
}
