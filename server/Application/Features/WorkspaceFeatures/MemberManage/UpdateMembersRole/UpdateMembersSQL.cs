namespace Application.Features.WorkspaceFeatures;

public static class UpdateMembersSQL
{
    public const string UpdateMemberRoles = @"
        UPDATE workspace_members AS wm
        SET role = COALESCE(NULLIF(t.role, ''), wm.role), 
            status = COALESCE(NULLIF(t.status, ''), wm.status), 
            updated_at = NOW()
        FROM (SELECT unnest(@UserIds) AS user_id, 
                     unnest(@Roles) AS role, 
                     unnest(@Statuses) AS status) AS t
        WHERE wm.user_id = t.user_id 
          AND wm.project_workspace_id = @WorkspaceId 
          AND wm.deleted_at IS NULL";
}
