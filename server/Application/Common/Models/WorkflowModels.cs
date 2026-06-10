namespace Application;

public record WorkflowRecord
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public Guid? ProjectSpaceId { get; init; }
    public Guid? ProjectFolderId { get; init; }
    public List<StatusRecord> Statuses { get; init; } = new();

    public static WorkflowRecord FromDomain(Workflow w) => new()
    {
        Id = w.Id,
        Name = w.Name,
        ProjectSpaceId = w.ProjectSpaceId,
        ProjectFolderId = w.ProjectFolderId
    };
}

public record StatusRecord
{
    public Guid Id { get; init; }
    public Guid WorkflowId { get; init; }
    public string Name { get; init; } = null!;
    public string? Color { get; init; }
    public StatusCategory Category { get; init; }
    public string? OrderKey { get; init; }

    public static StatusRecord FromDomain(Status s) => new()
    {
        Id = s.Id,
        WorkflowId = s.WorkflowId,
        Name = s.Name,
        Color = s.Color,
        Category = s.Category,
        OrderKey = s.OrderKey
    };
}

