namespace Application;

public record WorkflowRecord
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public Guid? ProjectSpaceId { get; init; }
    public Guid? ProjectFolderId { get; init; }
    
    public List<StatusRecord>? Statuses { get; init; } = null;
}

public record StatusRecord
{
    public Guid Id { get; init; }
    public Guid? StatusId { get; init; } // Alias for Dapper/SQL projections
    public string Name { get; init; } = null!;
    public string? Color { get; init; }
    public StatusCategory Category { get; init; }
    public string? OrderKey { get; init; }
}

public record StatusUpdateRecord(
    Guid? Id, 
    string Name,
    string Color,
    StatusCategory Category,
    string? PreviousOrderKey,
    string? NextOrderKey,
    RowAction Action
);
