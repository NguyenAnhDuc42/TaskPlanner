namespace Application.Features.WorkspaceFeatures;

public static class GetDetailWorkspaceSQL
{
    public const string GetDetail = @"
        SELECT 
            w.id AS WorkspaceId,
            wm.role AS Role,
            wm.theme AS Theme,
            w.custom_color AS Color,
            w.custom_icon AS Icon
        FROM project_workspaces w
        JOIN workspace_members wm ON w.id = wm.project_workspace_id
        WHERE w.id = @WorkspaceId 
          AND wm.user_id = @UserId
          AND w.deleted_at IS NULL
          AND wm.deleted_at IS NULL";
}

public record WorkspaceDetailRow(
    Guid WorkspaceId,
    string Role,
    string Theme,
    string Color,
    string Icon
);
