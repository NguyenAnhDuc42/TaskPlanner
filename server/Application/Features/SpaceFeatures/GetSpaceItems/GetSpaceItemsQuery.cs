namespace Application;

public record GetSpaceItemsQuery(Guid SpaceId, string? Cursor = null, int Limit = 200)
    : IQueryRequest<GetSpaceItemsResponse>, IAuthorizedWorkspaceRequest;

public record GetSpaceItemsResponse(
    List<FolderRecord> Folders,
    List<TaskRecord> Tasks,
    List<StatusRecord> Statuses,
    bool HasNextPage = false,
    string? NextCursor = null
);
