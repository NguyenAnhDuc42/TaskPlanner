namespace Application;

public record CommentRecord
{
    public Guid Id { get; init; }
    public string Content { get; init; } = null!;
    public Guid CreatorId { get; init; }
    public Guid? TaskId { get; init; }
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
    public Guid Id { get; init; }
    public Guid TaskId { get; init; }
    public Guid WorkspaceMemberId { get; init; }

    public static AssigneeRecord FromDomain(TaskAssignment a) => new()
    {
        Id = a.Id,
        TaskId = a.ProjectTaskId,
        WorkspaceMemberId = a.WorkspaceMemberId
    };
}
