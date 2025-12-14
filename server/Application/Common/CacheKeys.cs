namespace Application.Common;

/// <summary>
/// Centralized cache key definitions for HybridCache.
/// Used by Application handlers and Background jobs.
/// </summary>
public static class CacheKeys
{
    // Workspace
    public static string WorkspaceHierarchy(Guid id) => $"ws:{id}:hierarchy";
    public static string WorkspaceMembers(Guid id) => $"ws:{id}:members";
    public static string WorkspaceDetail(Guid id) => $"ws:{id}:detail";

    // User
    public static string UserWorkspaces(Guid userId) => $"user:{userId}:workspaces";

    // Lists
    public static string ListTasks(Guid listId) => $"list:{listId}:tasks";
}
