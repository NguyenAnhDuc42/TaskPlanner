namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public static class GetHierarchySql
{
    /// <summary>
    /// Fetches only the Spaces for a workspace. 
    /// Folders are lazy-loaded on expansion to handle 10k+ folder scale.
    /// </summary>
    public const string GetSpacesOnlyQuery = @"
       SELECT 
            s.id,
            s.name,
            s.custom_color AS color,
            s.custom_icon AS icon,
            s.is_private,
            s.order_key,
            CASE WHEN f.space_id IS NOT NULL THEN 1 ELSE 0 END AS has_folders,
            CASE WHEN t.space_id IS NOT NULL THEN 1 ELSE 0 END AS has_tasks
        FROM project_spaces s
        LEFT JOIN (
            SELECT DISTINCT project_space_id AS space_id
            FROM project_folders
            WHERE deleted_at IS NULL
              AND is_archived = false
        ) f ON f.space_id = s.id
        LEFT JOIN (
            SELECT DISTINCT project_space_id AS space_id
            FROM project_tasks
            WHERE deleted_at IS NULL
              AND is_archived = false
              AND project_folder_id IS NULL
        ) t ON t.space_id = s.id
        WHERE s.project_workspace_id = @WorkspaceId
          AND s.deleted_at IS NULL
          AND s.is_archived = false
        ORDER BY s.order_key, s.id;";

    public const string GetFoldersBySpaceQuery = @"
       SELECT 
            f.id,
            f.project_space_id AS parent_id,
            f.name,
            f.custom_color AS color,
            f.custom_icon AS icon,
            f.is_private,
            f.order_key,
            CASE WHEN t.folder_id IS NOT NULL THEN 1 ELSE 0 END AS has_tasks
        FROM project_folders f
        LEFT JOIN (
            SELECT DISTINCT project_folder_id AS folder_id
            FROM project_tasks
            WHERE deleted_at IS NULL
              AND is_archived = false
        ) t ON t.folder_id = f.id
        WHERE f.project_space_id = @SpaceId
          AND f.deleted_at IS NULL
          AND f.is_archived = false
        ORDER BY f.order_key, f.id;";
}
