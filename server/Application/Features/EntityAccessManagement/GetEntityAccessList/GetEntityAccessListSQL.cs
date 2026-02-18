namespace Application.Features.EntityAccessManagement.GetEntityAccessList;

public sealed class LayerHierarchy
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public int Level { get; set; }
}

public static class GetEntityAccessListSQL
{
    public const string GetAccessibleLayers = @"
WITH RECURSIVE layer_hierarchy AS (
    SELECT id, entity_type, parent_id, is_private, 1 AS level
    FROM (
        SELECT
            l.id,
            'ProjectList'::text AS entity_type,
            COALESCE(l.project_folder_id, l.project_space_id) AS parent_id,
            l.is_private
        FROM project_lists l
        JOIN project_spaces ps ON ps.id = l.project_space_id
        WHERE l.id = @EntityId
          AND @EntityType = 'ProjectList'
          AND l.deleted_at IS NULL
          AND ps.project_workspace_id = @WorkspaceId

        UNION ALL

        SELECT
            f.id,
            'ProjectFolder'::text AS entity_type,
            f.project_space_id AS parent_id,
            f.is_private
        FROM project_folders f
        JOIN project_spaces ps ON ps.id = f.project_space_id
        WHERE f.id = @EntityId
          AND @EntityType = 'ProjectFolder'
          AND f.deleted_at IS NULL
          AND ps.project_workspace_id = @WorkspaceId

        UNION ALL

        SELECT
            s.id,
            'ProjectSpace'::text AS entity_type,
            s.project_workspace_id AS parent_id,
            s.is_private
        FROM project_spaces s
        WHERE s.id = @EntityId
          AND @EntityType = 'ProjectSpace'
          AND s.deleted_at IS NULL
          AND s.project_workspace_id = @WorkspaceId

        UNION ALL

        SELECT
            w.id,
            'ProjectWorkspace'::text AS entity_type,
            NULL::uuid AS parent_id,
            FALSE AS is_private
        FROM project_workspaces w
        WHERE w.id = @EntityId
          AND @EntityType = 'ProjectWorkspace'
          AND w.deleted_at IS NULL
          AND w.id = @WorkspaceId
    ) start_layer

    UNION ALL

    SELECT
        f.id,
        'ProjectFolder'::text AS entity_type,
        f.project_space_id AS parent_id,
        f.is_private,
        lh.level + 1
    FROM layer_hierarchy lh
    JOIN project_folders f
      ON lh.entity_type = 'ProjectList'
     AND f.id = lh.parent_id
    WHERE lh.is_private = FALSE
      AND f.deleted_at IS NULL

    UNION ALL

    SELECT
        s.id,
        'ProjectSpace'::text AS entity_type,
        s.project_workspace_id AS parent_id,
        s.is_private,
        lh.level + 1
    FROM layer_hierarchy lh
    JOIN project_spaces s
      ON lh.entity_type = 'ProjectFolder'
     AND s.id = lh.parent_id
    WHERE lh.is_private = FALSE
      AND s.deleted_at IS NULL

    UNION ALL

    SELECT
        w.id,
        'ProjectWorkspace'::text AS entity_type,
        NULL::uuid AS parent_id,
        FALSE AS is_private,
        lh.level + 1
    FROM layer_hierarchy lh
    JOIN project_workspaces w
      ON lh.entity_type = 'ProjectSpace'
     AND w.id = lh.parent_id
    WHERE lh.is_private = FALSE
      AND w.deleted_at IS NULL
)
SELECT
    id,
    entity_type AS EntityType,
    is_private AS IsPrivate,
    level AS Level
FROM layer_hierarchy
ORDER BY level;
";
}
