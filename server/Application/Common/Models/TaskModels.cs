namespace Application;

public record CommentRecord
{
    public Guid Id { get; init; }
    public string Content { get; init; } = null!;
    public Guid CreatorId { get; init; }
    public Guid? ProjectTaskId { get; init; }
    public Guid? ParentCommentId { get; init; }
    public bool IsEdited { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public record AttachmentRecord
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Url { get; init; } = null!;
    public long SizeBytes { get; init; }
}

public record AssigneeRecord
{
    public Guid UserId { get; init; }
    public string UserName { get; init; } = null!;
    public string? AvatarUrl { get; init; }
}
