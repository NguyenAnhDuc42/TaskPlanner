namespace Application;

public record class GetDetailWorkspaceQuery(Guid WorkspaceId) : IQueryRequest<WorkspaceRecord>, IAuthorizedWorkspaceRequest;


