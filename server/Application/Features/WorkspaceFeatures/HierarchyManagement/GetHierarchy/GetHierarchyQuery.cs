namespace Application;

public record GetHierarchyQuery(Guid WorkspaceId) : IQueryRequest<WorkspaceHierarchyRecord>, IAuthorizedWorkspaceRequest;

// ── GetNodeFolders (lazy folder loading on expand) ──────────────────────────

public record GetNodeFoldersQuery(
    Guid WorkspaceId,
    Guid NodeId
) : IQueryRequest<List<FolderRecord>>, IAuthorizedWorkspaceRequest;


// ── GetNodeTasks (lazy task loading on expand) ──────────────────────────

public record GetNodeTasksQuery(
    Guid WorkspaceId,
    Guid ParentId,
    string ParentType,
    string? CursorOrderKey,
    string? CursorTaskId,
    int PageSize = 50
) : IQueryRequest<NodeTasksRecord>, IAuthorizedWorkspaceRequest;


