namespace Domain.Entities;

public static class StatusExtensions
{
    public static IQueryable<Status> ById(this IQueryable<Status> query, Guid id)
        => query.Where(s => s.Id == id);

    public static IQueryable<Status> ByWorkflow(this IQueryable<Status> query, Guid workflowId)
        => query.Where(s => s.WorkflowId == workflowId);

    public static IQueryable<Status> WhereNotDeleted(this IQueryable<Status> query) => 
        query.Where(s => s.DeletedAt == null);
}
