namespace Application.Features.TaskFeatures;

public static class GetTaskAssigneeCandidatesSQL
{
    public const string GetAssignedUserIds = @"
        SELECT wm.user_id
        FROM task_assignments ta
        JOIN workspace_members wm ON ta.workspace_member_id = wm.id
        WHERE ta.task_id = @TaskId
          AND ta.deleted_at IS NULL
          AND wm.deleted_at IS NULL";

    public const string GetCandidates = @"
        SELECT u.id AS UserId, u.name AS UserName, NULL AS AvatarUrl
        FROM workspace_members wm
        JOIN users u ON wm.user_id = u.id
        WHERE wm.project_workspace_id = @WorkspaceId
          AND wm.deleted_at IS NULL
          AND (@Search IS NULL OR u.name ILIKE ('%' || @Search || '%'))
          AND (array_length(@AssignedUserIds, 1) IS NULL OR NOT (u.id = ANY(@AssignedUserIds)))
        ORDER BY u.name
        LIMIT @Limit";
}
