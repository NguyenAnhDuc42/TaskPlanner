namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public static class GetNodeTasksSql
{
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
                WHEN t.project_folder_id IS NOT NULL THEN 'ProjectFolder'
                ELSE 'ProjectSpace'
            END as parent_type
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
              OR (t.order_key, t.id) > (@CursorOrderKey, @CursorTaskId::uuid)
          )
        ORDER BY t.order_key, t.id
        LIMIT @PageSize;";
}
