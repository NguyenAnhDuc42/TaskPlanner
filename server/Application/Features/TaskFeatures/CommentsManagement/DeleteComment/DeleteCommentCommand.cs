namespace Application;

public record DeleteCommentCommand(Guid TaskId, Guid CommentId) : ICommandRequest, IAuthorizedWorkspaceRequest;
