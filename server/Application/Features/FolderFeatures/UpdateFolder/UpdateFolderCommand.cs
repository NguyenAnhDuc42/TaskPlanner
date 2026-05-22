namespace Application;

public record UpdateFolderCommand(
    Guid FolderId,
    string? Name,
    string? Color,
    string? Icon,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? DueDate = null,
    Guid? StatusId = null,
    Priority? Priority = null
) : ICommandRequest, IAuthorizedWorkspaceRequest;


