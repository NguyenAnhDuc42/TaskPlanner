namespace Application;

public record SyncEventPayload(
    long SyncId,
    string Action,
    string EntityType,
    Guid EntityId,
    object Data,
    string? ClientTraceId
);

public record SyncDeltaBatch(
    List<SyncEventPayload> Actions,
    int DatabaseVersion,
    long LatestSyncId
);
