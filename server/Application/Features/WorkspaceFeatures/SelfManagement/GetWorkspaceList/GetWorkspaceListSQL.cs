using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Workspace;

namespace Application.Features.WorkspaceFeatures.SelfManagement.GetWorkspaceList;

public class WorkspaceRow
{
    public Guid Id { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public string Name { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Description { get; init; } = null!;
    public WorkspaceVariant Variant { get; init; }
    public Role Role { get; init; }
    public MembershipStatus MembershipStatus { get; init; }
    public int MemberCount { get; init; }
    public bool IsArchived { get; init; }
    public bool IsPinned { get; init; }
}

public static class GetWorkspaceListSQL
{
    public const string Asc = @"
    SELECT 
        w.id,
        w.updated_at AS UpdatedAt,
        w.name,
        w.custom_icon AS Icon,
        w.custom_color AS Color,
        w.description,
        w.variant,
        w.is_archived,
        wm.role,
        wm.status AS MembershipStatus,
        wm.is_pinned AS IsPinned,
        COUNT(wm_all.user_id) AS MemberCount
    FROM 
        project_workspaces w
    JOIN workspace_members wm 
        ON wm.project_workspace_id = w.id AND wm.user_id = @CurrentUserId AND wm.deleted_at IS NULL
    JOIN workspace_members wm_all
        ON wm_all.project_workspace_id = w.id AND wm_all.deleted_at IS NULL
    WHERE 
        w.deleted_at IS NULL AND
        (@name IS NULL OR w.name ILIKE '%' || @name || '%') AND 
        (@owned IS NULL OR @owned = false OR wm.role = 'Owner') AND
        (@isArchived IS NULL OR w.is_archived = @isArchived) AND 
        (@variant IS NULL OR w.variant = @variant) AND 
        (
            @cursorTimestamp IS NULL OR
                (
                    w.updated_at > @cursorTimestamp OR 
                    (w.updated_at = @cursorTimestamp AND w.id > @cursorId)
                )
        )
    GROUP BY
        w.id, w.name, w.custom_icon, w.custom_color, w.description, w.variant, w.is_archived, wm.role, wm.status, w.updated_at, wm.is_pinned
    ORDER BY
        wm.is_pinned DESC, w.updated_at ASC, w.id ASC
    LIMIT @PageSizePLusOne;
    ";

    public const string Desc = @"
    SELECT 
        w.id,
        w.updated_at AS UpdatedAt,
        w.name,
        w.custom_icon AS Icon,
        w.custom_color AS Color,
        w.description,
        w.variant,
        w.is_archived,
        wm.role,
        wm.status AS MembershipStatus,
        wm.is_pinned AS IsPinned,
        COUNT(wm_all.user_id) AS MemberCount
    FROM 
        project_workspaces w
    JOIN workspace_members wm 
        ON wm.project_workspace_id = w.id AND wm.user_id = @CurrentUserId AND wm.deleted_at IS NULL
    JOIN workspace_members wm_all
        ON wm_all.project_workspace_id = w.id AND wm_all.deleted_at IS NULL
    WHERE 
        w.deleted_at IS NULL AND
        (@name IS NULL OR w.name ILIKE '%' || @name || '%') AND 
        (@owned IS NULL OR @owned = false OR wm.role = 'Owner') AND
        (@isArchived IS NULL OR w.is_archived = @isArchived) AND 
        (@variant IS NULL OR w.variant = @variant) AND 
        (
            @cursorTimestamp IS NULL OR
                (
                    w.updated_at < @cursorTimestamp OR 
                    (w.updated_at = @cursorTimestamp AND w.id < @cursorId)
                )
        )
    GROUP BY
        w.id, w.name, w.custom_icon, w.custom_color, w.description, w.variant, w.is_archived, wm.role, wm.status, w.updated_at, wm.is_pinned
    ORDER BY
        wm.is_pinned DESC, w.updated_at DESC, w.id DESC
    LIMIT @PageSizePLusOne;
    ";
}
