namespace Application;

public record GetFolderDetailQuery(Guid FolderId) : IQueryRequest<FolderDetailResponse>, IAuthorizedWorkspaceRequest;

public record FolderDetailResponse(
    FolderRecord Folder,
    BreadcrumbInfo Space,
    StatusRecord? FolderStatus,
    Guid? ParentWorkflowId,           // space workflow — folder status selector
    List<StatusRecord> SpaceStatuses, // space workflow statuses — for folder status badge/select
    Guid? WorkflowId,                 // folder's own workflow — for Workflow button
    List<StatusRecord> TaskStatuses   // folder's own workflow statuses — for child tasks
);
