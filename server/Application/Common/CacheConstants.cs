using System;

namespace Application.Common;

public static class CacheConstants
{
    // Tags for bulk invalidation
    public static class Tags
    {
        public static string UserPermissions(Guid userId) => $"user:{userId}:permissions";
        public static string WorkspaceMembers(Guid workspaceId) => $"workspaces:{workspaceId}:members";
        public static string UserWorkspaces(Guid userId) => $"user:{userId}:workspaces";
    }

    // Keys for specific items
    public static class Keys
    {
        public static string WorkspaceMemberRole(Guid userId, Guid workspaceId) 
            => $"workspaces:{workspaceId}:user:{userId}:perm";

        public static string EntityAccessLevel(Guid userId, Guid workspaceId, Guid entityId, string entityType) 
            => $"workspaces:{workspaceId}:user:{userId}:entity:{entityId}:perm";
    }
}
