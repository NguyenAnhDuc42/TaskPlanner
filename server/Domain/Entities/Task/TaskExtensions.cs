using System;
using System.Linq;

namespace Domain.Entities;

public static class TaskExtensions
{
    // Instance Extensions
    public static bool IsActive(this ProjectTask task) => !task.IsArchived;

    // Queryable Extensions
    public static IQueryable<ProjectTask> WhereActive(this IQueryable<ProjectTask> query) => 
        query.Where(task => !task.IsArchived);

    public static IQueryable<ProjectTask> ById(this IQueryable<ProjectTask> query, Guid id)
        => query.Where(t => t.Id == id);

    public static IQueryable<ProjectTask> ByWorkspace(this IQueryable<ProjectTask> query, Guid workspaceId)
        => query.Where(t => t.ProjectWorkspaceId == workspaceId);

    public static IQueryable<ProjectTask> BySpace(this IQueryable<ProjectTask> query, Guid spaceId)
        => query.Where(t => t.ProjectSpaceId == spaceId);

    public static IQueryable<ProjectTask> ByFolder(this IQueryable<ProjectTask> query, Guid folderId)
        => query.Where(t => t.ProjectFolderId == folderId);

    public static IQueryable<ProjectTask> BySlug(this IQueryable<ProjectTask> query, string slug)
        => query.Where(t => t.Slug == slug);
}
