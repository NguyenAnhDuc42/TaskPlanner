namespace Api;

public record GetEntityChangesQuery(Guid EntityId, SyncEntityType EntityType) : IQueryRequest<List<ChangeEntryRecord>>, IAuthorizedWorkspaceRequest;
