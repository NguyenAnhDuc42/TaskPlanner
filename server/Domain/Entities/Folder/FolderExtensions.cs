using System;
using System.Linq;
using Domain.Entities.ProjectEntities;

namespace Domain.Entities;

public static class FolderExtensions
{
    // Instance Extensions
    public static bool IsActive(this ProjectFolder folder) => 
        !folder.IsArchived;

    // Queryable Extensions
    public static IQueryable<ProjectFolder> WhereActive(this IQueryable<ProjectFolder> query) => 
        query.Where(folder => !folder.IsArchived);

    public static IQueryable<ProjectFolder> BySpace(this IQueryable<ProjectFolder> query, Guid spaceId) => 
        query.Where(folder => folder.ProjectSpaceId == spaceId);
}
