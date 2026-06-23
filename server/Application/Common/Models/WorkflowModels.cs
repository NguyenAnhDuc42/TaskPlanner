namespace Application;

public record StatusRecord
{
    public Guid Id { get; init; }
    public Guid SpaceId { get; init; }
    public string Name { get; init; } = null!;
    public string? Color { get; init; }
    public StatusCategory Category { get; init; }
    public string? OrderKey { get; init; }

    public static StatusRecord FromDomain(Status s) => new()
    {
        Id = s.Id,
        SpaceId = s.ProjectSpaceId,
        Name = s.Name,
        Color = s.Color,
        Category = s.Category,
        OrderKey = s.OrderKey
    };
}
