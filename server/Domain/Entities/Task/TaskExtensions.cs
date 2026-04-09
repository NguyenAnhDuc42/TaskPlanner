using System;
using System.Linq;
using Domain.Entities.ProjectEntities;

namespace Domain.Entities;

public static class TaskExtensions
{
    // Instance Extensions
    public static bool IsActive(this ProjectTask task) => 
        !task.IsArchived;

    // Queryable Extensions
    public static IQueryable<ProjectTask> WhereActive(this IQueryable<ProjectTask> query) => 
        query.Where(task => !task.IsArchived);

    public static IQueryable<ProjectTask> ByFolder(this IQueryable<ProjectTask> query, Guid folderId) => 
        query.Where(task => task.ProjectFolderId == folderId);

    public static IQueryable<ProjectTask> BySpace(this IQueryable<ProjectTask> query, Guid spaceId) => 
        query.Where(task => task.ProjectSpaceId == spaceId);
        
    public static IQueryable<ProjectTask> ByWorkspace(this IQueryable<ProjectTask> query, Guid workspaceId) => 
        query.Where(task => task.ProjectWorkspaceId == workspaceId);
}
