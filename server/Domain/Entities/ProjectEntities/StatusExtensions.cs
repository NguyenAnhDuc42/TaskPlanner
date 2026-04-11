using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entities.ProjectEntities;

public static class StatusExtensions
{
    public static IQueryable<Status> ById(this IQueryable<Status> query, Guid id)
        => query.Where(s => s.Id == id);

    public static IQueryable<Status> ByWorkflow(this IQueryable<Status> query, Guid workflowId)
        => query.Where(s => s.WorkflowId == workflowId);

    public static IQueryable<Status> ByWorkspace(this IQueryable<Status> query, Guid workspaceId)
        => query.Where(s => s.ProjectWorkspaceId == workspaceId);

    public static IQueryable<Workflow> ById(this IQueryable<Workflow> query, Guid id)
        => query.Where(w => w.Id == id);

    public static IQueryable<Workflow> ByWorkspace(this IQueryable<Workflow> query, Guid workspaceId)
        => query.Where(w => w.ProjectWorkspaceId == workspaceId);

    public static IQueryable<Workflow> BySpace(this IQueryable<Workflow> query, Guid spaceId)
        => query.Where(w => w.SpaceId == spaceId);

    public static IQueryable<Workflow> ByFolder(this IQueryable<Workflow> query, Guid folderId)
        => query.Where(w => w.FolderId == folderId);
}
