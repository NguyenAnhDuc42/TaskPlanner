namespace Application;

public record AddCommentCommand(Guid TaskId, string Content, Guid? ParentCommentId = null) : ICommandRequest<CommentDto>, IAuthorizedWorkspaceRequest;

public record AddCommentRequest(string Content, Guid? ParentCommentId = null);


