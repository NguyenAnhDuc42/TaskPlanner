using System.Linq;
using Domain.Entities.ProjectEntities;

namespace Domain.Entities;

public static class WorkspaceExtensions
{
    // Instance Extensions
    public static bool IsActive(this ProjectWorkspace workspace) => 
        !workspace.IsArchived;

    // Queryable Extensions
    public static IQueryable<ProjectWorkspace> WhereActive(this IQueryable<ProjectWorkspace> query) => 
        query.Where(workspace => !workspace.IsArchived);

    public static IQueryable<ProjectWorkspace> BySlug(this IQueryable<ProjectWorkspace> query, string slug) => 
        query.Where(workspace => workspace.Slug == slug.ToLower().Trim());

    public static IQueryable<ProjectWorkspace> ByJoinCode(this IQueryable<ProjectWorkspace> query, string joinCode) => 
        query.Where(workspace => workspace.JoinCode == joinCode.Trim());
}
