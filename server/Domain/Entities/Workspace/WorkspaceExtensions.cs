using System.Linq;

namespace Domain.Entities;

public static class WorkspaceExtensions
{
    // Instance Extensions
    public static bool IsActive(this ProjectWorkspace workspace) => 
        !workspace.IsArchived;

    public static IQueryable<ProjectWorkspace> ById(this IQueryable<ProjectWorkspace> query, Guid id) => 
        query.Where(workspace => workspace.Id == id);

    public static IQueryable<ProjectWorkspace> WhereActive(this IQueryable<ProjectWorkspace> query) => 
        query.Where(workspace => !workspace.IsArchived);

    public static IQueryable<ProjectWorkspace> WhereNotDeleted(this IQueryable<ProjectWorkspace> query) => 
        query.Where(workspace => workspace.DeletedAt == null);

    public static IQueryable<ProjectWorkspace> BySlug(this IQueryable<ProjectWorkspace> query, string slug) => 
        query.Where(workspace => workspace.Slug == slug.ToLower().Trim());

    public static IQueryable<ProjectWorkspace> ByJoinCode(this IQueryable<ProjectWorkspace> query, string joinCode) => 
        query.Where(workspace => workspace.JoinCode == joinCode.Trim());
}
