namespace Api;

public record GetChangesQuery(Guid WorkspaceId, long Since) : IQueryRequest<SyncDeltaBatch>, IAuthorizedWorkspaceRequest;
