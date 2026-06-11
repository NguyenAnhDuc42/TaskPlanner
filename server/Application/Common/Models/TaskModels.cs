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

    public static CommentRecord FromDomain(Comment c) => new()
    {
        Id = c.Id,
        Content = c.Content,
        CreatorId = c.CreatorId ?? Guid.Empty,
        TaskId = c.ProjectTaskId,
        ParentCommentId = c.ParentCommentId,
        IsEdited = c.IsEdited,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}

public record AttachmentRecord
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Url { get; init; } = null!;
    public long SizeBytes { get; init; }

    public static AttachmentRecord FromDomain(Attachment a, EntityAssetLink? link = null) => new()
    {
        Id = a.Id,
        Name = a.FileName,
        Url = a.StoragePath,
        SizeBytes = a.SizeBytes
    };
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
