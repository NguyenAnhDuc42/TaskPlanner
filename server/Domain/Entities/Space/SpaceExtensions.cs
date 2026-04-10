using System;
using System.Linq;
using Domain.Entities.ProjectEntities;

namespace Domain.Entities;

public static class SpaceExtensions
{
    // Instance Extensions
    public static bool IsActive(this ProjectSpace space) => 
        !space.IsArchived;

    // Queryable Extensions
    public static IQueryable<ProjectSpace> WhereActive(this IQueryable<ProjectSpace> query) => 
        query.Where(space => !space.IsArchived);

    public static IQueryable<ProjectSpace> ByWorkspace(this IQueryable<ProjectSpace> query, Guid workspaceId) => 
        query.Where(space => space.ProjectWorkspaceId == workspaceId);

    public static IQueryable<ProjectSpace> BySlug(this IQueryable<ProjectSpace> query, string slug) => 
        query.Where(space => space.Slug == slug.ToLower().Trim());

    public static IQueryable<ProjectSpace> WhereNotDeleted(this IQueryable<ProjectSpace> query) => 
        query.Where(space => space.DeletedAt == null);
}
