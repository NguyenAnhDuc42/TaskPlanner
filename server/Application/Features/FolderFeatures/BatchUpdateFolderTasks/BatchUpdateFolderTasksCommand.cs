namespace Application;

public record BatchUpdateFolderTasksCommand(
    Guid FolderId,
    List<BatchUpdateFolderTaskValue> Updates
) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record BatchUpdateFolderTaskValue(
    Guid Id,
    Guid? StatusId = null,
    Priority? Priority = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? DueDate = null,
    string? OrderKey = null,
    bool? IsDeleted = null,
    bool ClearStartDate = false,
    bool ClearDueDate = false
);
