namespace Api;

public record class GetDetailWorkspaceQuery(Guid WorkspaceId) : IQueryRequest<WorkspaceRecord>, IAuthorizedWorkspaceRequest;
