namespace Application;

public record ReorderFavoriteCommand(
    Guid EntityId,
    EntityLayerType EntityLayerType,
    string? PreviousOrderKey,
    string? NextOrderKey
) : ICommandRequest, IAuthorizedWorkspaceRequest;
