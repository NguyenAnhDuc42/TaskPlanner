namespace Application;

public record DeleteFolderCommand(Guid FolderId) : ICommandRequest, IAuthorizedWorkspaceRequest;


