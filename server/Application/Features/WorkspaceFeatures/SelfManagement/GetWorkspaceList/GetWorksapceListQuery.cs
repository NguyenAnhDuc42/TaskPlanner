namespace Application;

public record class GetWorksapceListQuery(CursorPaginationRequest Pagination, WorkspaceFilter filter) : IQueryRequest<PagedResult<WorkspaceRecord>>;

public record WorkspaceFilter(string? Name = null, bool? Owned = null, bool? isArchived = null);
