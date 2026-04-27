using System.Linq;

namespace Domain.Entities;

public static class WorkflowExtensions
{
    public static IQueryable<Workflow> ById(this IQueryable<Workflow> query, Guid id)
        => query.Where(w => w.Id == id);

    public static IQueryable<Workflow> ByWorkspace(this IQueryable<Workflow> query, Guid workspaceId)
        => query.Where(w => w.ProjectWorkspaceId == workspaceId);

    public static IQueryable<Workflow> BySpace(this IQueryable<Workflow> query, Guid spaceId)
        => query.Where(w => w.SpaceId == spaceId);

    public static IQueryable<Workflow> ByFolder(this IQueryable<Workflow> query, Guid folderId)
        => query.Where(w => w.FolderId == folderId);

    public static IQueryable<Workflow> WhereNotDeleted(this IQueryable<Workflow> query) => 
        query.Where(s => s.DeletedAt == null);
}