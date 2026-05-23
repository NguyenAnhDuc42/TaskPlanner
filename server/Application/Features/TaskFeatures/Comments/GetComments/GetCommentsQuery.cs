namespace Application;

public record GetCommentsQuery(Guid TaskId) : IQueryRequest<List<CommentRecord>>, IAuthorizedWorkspaceRequest;


