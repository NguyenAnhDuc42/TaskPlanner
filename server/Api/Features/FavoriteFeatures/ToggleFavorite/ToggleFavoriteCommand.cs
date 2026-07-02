using System.Text.Json.Serialization;

namespace Api;

// Backend-first, no SyncEvent/broadcast — favorites are personal (per WorkspaceMember) and the
// shared SyncHub group broadcasts to the whole workspace, so treating this like Task/Comment
// would leak "who favorited what" to every other member. Same bucket as Workspace mutations.
// OrderKey is required — computed client-side (FractionalIndex), used only when this toggle adds
// a new favorite; ignored when it removes one.
public record ToggleFavoriteCommand(
    Guid EntityId,
    EntityLayerType EntityLayerType,
    string OrderKey
) : ICommandRequest<ToggleFavoriteResult>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}

public record ToggleFavoriteResult(bool IsFavorite, string? FavoriteOrderKey, Guid EntityId, EntityLayerType EntityLayerType);
