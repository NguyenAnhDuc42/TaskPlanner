namespace Application;

public record class CreateFolderCommand(
    Guid spaceId,
    string name,
    string color,
    string icon,
    Guid? statusId = null,
    DateTimeOffset? startDate = null,
    DateTimeOffset? dueDate = null
) : ICommandRequest<Guid>, IAuthorizedWorkspaceRequest;

