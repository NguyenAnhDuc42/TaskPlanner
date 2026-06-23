namespace Application;

public record GetWorkspaceStatusesQuery() : IQueryRequest<List<StatusRecord>>, IAuthorizedWorkspaceRequest;
