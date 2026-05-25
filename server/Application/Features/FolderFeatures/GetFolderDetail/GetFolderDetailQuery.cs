namespace Application;

public record GetFolderDetailQuery(Guid FolderId) : IQueryRequest<FolderDetailResponse>, IAuthorizedWorkspaceRequest;

public record FolderDetailResponse(
    FolderRecord Folder,
    List<StatusRecord> Statuses,
    Guid? WorkflowId
);


