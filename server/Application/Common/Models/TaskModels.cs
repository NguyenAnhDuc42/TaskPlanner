namespace Application;

public record TaskRecord(
    Guid Id,
    string Name,
    string? Description = null,
    Priority? Priority = null,
    Guid? StatusId = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? DueDate = null,
    string? OrderKey = null,
    string? Icon = null,
    string? Color = null,
    List<CommentRecord>? Comments = null,
    List<AttachmentRecord>? Attachments = null
);

public record CommentRecord(
    Guid Id,
    string Content,
    Guid UserId,
    DateTimeOffset CreatedAt
);

public record AttachmentRecord(
    Guid Id,
    string Name,
    string Url,
    long SizeBytes
);
