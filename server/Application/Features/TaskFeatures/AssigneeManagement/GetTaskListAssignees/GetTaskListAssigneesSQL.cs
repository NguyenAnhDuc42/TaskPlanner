namespace Application.Features.TaskFeatures;

public static class GetTaskListAssigneesSQL
{
    public const string GetWorkspaceMembers = @"
        SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
        FROM workspace_members wm
        JOIN users u ON wm.user_id = u.id
        WHERE wm.project_workspace_id = @WorkspaceId
          AND wm.deleted_at IS NULL
        ORDER BY u.name";
}
