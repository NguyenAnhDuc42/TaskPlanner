namespace Application.Features.WorkspaceFeatures;

public static class RemoveMembersSQL
{
    public const string RemoveMembers = @"
        UPDATE workspace_members 
        SET deleted_at = NOW(), 
            updated_at = NOW() 
        WHERE project_workspace_id = @WorkspaceId 
          AND user_id = ANY(@UserIds)
          AND deleted_at IS NULL";
}
