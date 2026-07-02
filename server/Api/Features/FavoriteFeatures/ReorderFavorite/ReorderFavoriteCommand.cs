namespace Api;

// Backend-first, no SyncEvent/broadcast — same reasoning as ToggleFavorite. OrderKey is
// computed client-side; server just persists it.
public record ReorderFavoriteCommand(
    Guid EntityId,
    EntityLayerType EntityLayerType,
    string OrderKey
) : ICommandRequest, IAuthorizedWorkspaceRequest;
