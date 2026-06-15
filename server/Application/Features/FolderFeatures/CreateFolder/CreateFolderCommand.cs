namespace Application;

public record class CreateFolderCommand(
    Guid SpaceId,
    string Name,
    string Color,
    string Icon,
    Guid? StatusId = null,
    Priority? Priority = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? DueDate = null
) : ICommandRequest<Guid>, IAuthorizedWorkspaceRequest;

