namespace Application;

public record UnFavoriteTaskCommand(Guid FavoriteId) : ICommandRequest,IAuthorizedWorkspaceRequest;