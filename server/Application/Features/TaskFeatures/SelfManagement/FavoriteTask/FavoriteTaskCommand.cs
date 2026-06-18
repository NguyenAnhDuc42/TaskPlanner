namespace Application;

public record FavoriteTaskCommand(Guid TaskId) : ICommandRequest<TaskRecord>,IAuthorizedWorkspaceRequest;