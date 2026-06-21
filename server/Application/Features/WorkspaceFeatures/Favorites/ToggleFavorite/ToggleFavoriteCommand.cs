using Domain;

namespace Application;

public record ToggleFavoriteCommand(Guid EntityId, EntityLayerType EntityLayerType) : ICommandRequest<ToggleFavoriteResponse>, IAuthorizedWorkspaceRequest;
