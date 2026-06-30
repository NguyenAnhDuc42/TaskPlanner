namespace Api;

public record GetBootstrapQuery(Guid WorkspaceId) : IQueryRequest<BootstrapResult>, IAuthorizedWorkspaceRequest;

public record BootstrapResult(
    long LastSyncId,
    int DatabaseVersion,
    List<TaskRecord> Tasks
);
