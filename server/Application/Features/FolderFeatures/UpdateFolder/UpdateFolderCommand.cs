namespace Application;

public record UpdateFolderCommand(
    Guid FolderId,
    string? Name,
    string? Color,
    string? Icon,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? DueDate = null,
    string? OrderKey = null,
    bool ClearStartDate = false,
    bool ClearDueDate = false
) : ICommandRequest, IAuthorizedWorkspaceRequest;
