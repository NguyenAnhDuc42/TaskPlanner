namespace Application;

public record GetCommentsQuery(Guid TaskId, CursorPaginationRequest Pagination) : IQueryRequest<PagedResult<CommentRecord>>, IAuthorizedWorkspaceRequest;
