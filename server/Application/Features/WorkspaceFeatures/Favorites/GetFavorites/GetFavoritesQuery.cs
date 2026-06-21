namespace Application;

public record GetFavoritesQuery(CursorPaginationRequest Pagination)
    : IQueryRequest<GetFavoritesResponse>, IAuthorizedWorkspaceRequest;

public record GetFavoritesResponse(
    List<SpaceRecord>  Spaces,
    List<FolderRecord> Folders,
    List<TaskRecord>   Tasks,
    string?            NextCursor,
    bool               HasNextPage
);
