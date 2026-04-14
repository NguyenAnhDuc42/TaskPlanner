using System;
using System.Linq;
using Domain.Entities.ProjectEntities;

namespace Domain.Entities;

public static class SpaceExtensions
{
    // Instance Extensions
    public static bool IsActive(this ProjectSpace space) => 
        !space.IsArchived;

    public static IQueryable<ProjectSpace> ById(this IQueryable<ProjectSpace> query, Guid id) => 
        query.Where(space => space.Id == id);

    public static IQueryable<ProjectSpace> WhereActive(this IQueryable<ProjectSpace> query) => 
        query.Where(space => !space.IsArchived);

    public static IQueryable<ProjectSpace> ByWorkspace(this IQueryable<ProjectSpace> query, Guid workspaceId) => 
        query.Where(space => space.ProjectWorkspaceId == workspaceId);

    public static IQueryable<ProjectSpace> BySlug(this IQueryable<ProjectSpace> query, string slug) => 
        query.Where(space => space.Slug == slug.ToLower().Trim());
}
