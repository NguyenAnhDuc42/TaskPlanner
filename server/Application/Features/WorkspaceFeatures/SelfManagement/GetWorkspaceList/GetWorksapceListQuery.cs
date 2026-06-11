namespace Application;

public record class GetWorksapceListQuery(CursorPaginationRequest Pagination, WorkspaceFilter filter) : IQueryRequest<PagedResult<WorkspaceSnippetRecord>>;

public record WorkspaceFilter(string? Name = null, bool? Owned = null, bool? isArchived = null);
