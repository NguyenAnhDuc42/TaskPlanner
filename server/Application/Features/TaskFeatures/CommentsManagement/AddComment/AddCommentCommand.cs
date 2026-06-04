namespace Application;

public record AddCommentCommand(Guid TaskId, string Content, Guid? ParentCommentId = null) : ICommandRequest<CommentRecord>, IAuthorizedWorkspaceRequest;

public record AddCommentRequest(string Content, Guid? ParentCommentId = null);


