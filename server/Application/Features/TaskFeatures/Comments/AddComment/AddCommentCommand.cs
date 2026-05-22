using Application.Common.Interfaces;

namespace Application.Features.TaskFeatures;

public record AddCommentCommand(Guid TaskId, string Content, Guid? ParentCommentId = null) : ICommandRequest<CommentDto>, IAuthorizedWorkspaceRequest;

public record AddCommentRequest(string Content, Guid? ParentCommentId = null);
