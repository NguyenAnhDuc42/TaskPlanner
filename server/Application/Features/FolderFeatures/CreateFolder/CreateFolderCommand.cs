namespace Application;

public record class CreateFolderCommand(
    Guid SpaceId,
    string Name,
    string Color,
    string Icon,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? DueDate = null
) : ICommandRequest<FolderRecord>, IAuthorizedWorkspaceRequest;
