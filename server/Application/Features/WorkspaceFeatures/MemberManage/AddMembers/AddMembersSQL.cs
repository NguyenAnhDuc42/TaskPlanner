namespace Application.Features.WorkspaceFeatures;

public static class AddMembersSQL
{
    public const string BulkAddMembers = @"
        INSERT INTO workspace_members (
            id, user_id, project_workspace_id, role, status, 
            creator_id, created_at, updated_at, theme, joined_at, join_method
        )
        SELECT 
            gen_random_uuid(), 
            u.id, 
            @WorkspaceId, 
            t.role, 
            'Active', 
            @CreatorId, 
            NOW(), 
            NOW(), 
            @Theme,
            NOW(),
            'Invite'
        FROM users u
        JOIN (
            SELECT unnest(@Emails) AS email, 
                   unnest(@Roles) AS role
        ) AS t ON u.email = t.email
        LEFT JOIN workspace_members wm ON u.id = wm.user_id AND wm.project_workspace_id = @WorkspaceId
        WHERE wm.id IS NULL 
          AND u.deleted_at IS NULL;";
}
