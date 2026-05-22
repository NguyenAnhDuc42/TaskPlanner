namespace Application;

public record GetWorkspaceWorkflowsQuery(Guid? LayerId = null, string? LayerType = null) : IQueryRequest<List<WorkflowRecord>>, IAuthorizedWorkspaceRequest;


