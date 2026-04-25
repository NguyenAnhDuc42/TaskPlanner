namespace Application.Features.TaskFeatures;

public static class GetTaskAssigneesSQL
{
    public const string GetAssignees = @"
        SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
        FROM task_assignments ta
        JOIN workspace_members wm ON ta.workspace_member_id = wm.id
        JOIN users u ON wm.user_id = u.id
        WHERE ta.task_id = @TaskId
          AND ta.deleted_at IS NULL
          AND wm.deleted_at IS NULL
        ORDER BY u.name";
}
