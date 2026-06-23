namespace Application;

public record GetFolderDetailQuery(Guid FolderId) : IQueryRequest<FolderDetailResponse>, IAuthorizedWorkspaceRequest;

public record FolderDetailResponse(
    FolderRecord Folder,
    BreadcrumbInfo Space,
    List<StatusRecord> SpaceStatuses
);
