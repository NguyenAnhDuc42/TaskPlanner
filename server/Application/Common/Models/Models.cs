namespace Application;

public record TaskRecord
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? StatusId { get; init; }
    public Priority? Priority { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public string? OrderKey { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    
    // Detailed properties
    public Guid? ProjectSpaceId { get; init; }
    public Guid? ProjectFolderId { get; init; }
    public string? Description { get; init; }
    public Guid? ParentWorkflowId { get; init; }
    public Guid? DefaultDocumentId { get; init; }
    public bool? IsArchived { get; init; }
    public int? StoryPoints { get; init; }
    public long? TimeEstimateSeconds { get; init; }
    public string? ParentType { get; init; }
    public List<Guid>? AssigneeIds { get; init; } = null;
}

public record FolderRecord
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? StatusId { get; init; }
    public Priority? Priority { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public string? OrderKey { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public bool? IsPrivate { get; init; }
    public bool? HasTasks { get; init; }
    public Guid? ParentId { get; init; }
}

public record TaskViewData(
    List<FolderRecord> Folders,
    List<TaskRecord> Tasks,
    List<StatusRecord> Statuses
);

public record DocumentBlockRecord
{
    public Guid Id { get; init; }
    public BlockType Type { get; init; }
    public string Content { get; init; } = null!;
    public string OrderKey { get; init; } = null!;
}
