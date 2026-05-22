using Application.Common.Interfaces;

namespace Application.Features.TaskFeatures;

public record GetCommentsQuery(Guid TaskId) : IQueryRequest<List<CommentDto>>, IAuthorizedWorkspaceRequest;

public record CommentDto
{
    public Guid Id { get; init; }
    public string Content { get; init; } = null!;
    public Guid CreatorId { get; init; }
    public Guid ProjectTaskId { get; init; }
    public Guid? ParentCommentId { get; init; }
    public bool IsEdited { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
