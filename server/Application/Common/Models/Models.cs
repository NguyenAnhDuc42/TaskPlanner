namespace Application;

public record TaskRecord
{
    public Guid Id { get; init; }
    public Guid? WorkspaceId { get; init; }       
    public string Name { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? StatusId { get; init; }
    public Priority? Priority { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public string? OrderKey { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    
    // Ancestor ids
    public Guid? SpaceId { get; init; }
    public Guid? FolderId { get; init; }

    // Detailed properties
    public Guid? ParentWorkflowId { get; init; }
    public Guid? DefaultDocumentId { get; init; }
    public bool? IsArchived { get; init; }
    public int? StoryPoints { get; init; }
    public long? TimeEstimateSeconds { get; init; }
    public string? ParentType { get; init; }
    public Guid? ParentTaskId { get; init; }
    public bool? IsFavorite { get; init; }
    public string? FavoriteOrderKey { get; init; }
    public AccessLevel? AccessLevel { get; init; }

    public static TaskRecord FromDomain(ProjectTask t) => new()
    {
        Id = t.Id,
        WorkspaceId = t.ProjectWorkspaceId,
        Name = t.Name,
        CreatedAt = t.CreatedAt,
        StatusId = t.StatusId,
        Priority = t.Priority,
        StartDate = t.StartDate,
        DueDate = t.DueDate,
        OrderKey = t.OrderKey,
        Icon = t.Icon,
        Color = t.Color,
        SpaceId = t.ProjectSpaceId,
        FolderId = t.ProjectFolderId,
        DefaultDocumentId = t.DefaultDocumentId,
        IsArchived = t.IsArchived,
        StoryPoints = t.StoryPoints,
        TimeEstimateSeconds = t.TimeEstimateSeconds,
        ParentType = t.ProjectFolderId != null ? "ProjectFolder" : "ProjectSpace",
        ParentTaskId = t.ParentTaskId
    };
}

public record FolderRecord
{
    public Guid Id { get; init; }
    public Guid? WorkspaceId { get; init; }
    public Guid? SpaceId { get; init; }
    public string Name { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? StatusId { get; init; }
    public Priority? Priority { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public string? OrderKey { get; init; }
    public string? Icon { get; init; }
    public string? Color { get; init; }
    public bool? HasTasks { get; init; }
    public Guid? WorkflowId { get; init; }
    public AccessLevel? AccessLevel { get; init; }
    public bool? IsFavorite { get; init; }
    public string? FavoriteOrderKey { get; init; }

    public static FolderRecord FromDomain(ProjectFolder f, Guid? workflowId) => new()
    {
        Id = f.Id,
        WorkspaceId = f.ProjectWorkspaceId,
        SpaceId = f.ProjectSpaceId,
        Name = f.Name,
        CreatedAt = f.CreatedAt,
        StatusId = f.StatusId,
        Priority = f.Priority,
        StartDate = f.StartDate,
        DueDate = f.DueDate,
        OrderKey = f.OrderKey,
        Icon = f.Icon,
        Color = f.Color,
        HasTasks = null,
        WorkflowId = workflowId
    };
}


public record DocumentBlockRecord
{
    public Guid Id { get; init; }
    public BlockType Type { get; init; }
    public string Content { get; init; } = null!;
    public string OrderKey { get; init; } = null!;
}

public record BreadcrumbInfo(string Name, string? Icon, string? Color);
