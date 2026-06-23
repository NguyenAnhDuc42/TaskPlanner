namespace Domain;

public class Status : TenantEntity
{
    public Guid ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public StatusCategory Category { get; private set; }
    public string OrderKey { get; private set; } = null!;

    private Status() { } // EF Core

    private Status(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, string name, string color, StatusCategory category, Guid creatorId, string orderKey)
        : base(id, projectWorkspaceId)
    {
        ProjectSpaceId = projectSpaceId;
        Name = name;
        Color = color;
        Category = category;
        OrderKey = orderKey;
        InitializeAudit(creatorId);
    }

    public static Status Create(Guid projectWorkspaceId, Guid projectSpaceId, string name, string color, StatusCategory category, Guid creatorId, string? orderKey = null, Guid? id = null)
        => new Status(id ?? Guid.NewGuid(), projectWorkspaceId, projectSpaceId, name, color, category, creatorId, orderKey ?? FractionalIndex.Start());

    public static List<Status> CreateSpaceStarterSet(Guid projectWorkspaceId, Guid projectSpaceId, Guid creatorId)
    {
        var start = FractionalIndex.Start();
        var key2 = FractionalIndex.After(start);
        var key3 = FractionalIndex.After(key2);
        var key4 = FractionalIndex.After(key3);

        return new List<Status>
        {
            new Status(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, "Planned",     "#808080", StatusCategory.NotStarted, creatorId, start),
            new Status(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, "In Progress", "#1e90ff", StatusCategory.Active,     creatorId, key2),
            new Status(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, "Paused",      "#ff8c00", StatusCategory.Active,     creatorId, key3),
            new Status(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, "Completed",   "#008000", StatusCategory.Done,       creatorId, key4)
        };
    }

    public void Update(
        string? name = null,
        string? color = null,
        StatusCategory? category = null,
        string? orderKey = null)
    {
        bool updated = false;

        if (name != null && Name != name) { Name = name; updated = true; }
        if (color != null && Color != color) { Color = color; updated = true; }
        if (category != null && Category != category) { Category = category.Value; updated = true; }
        if (orderKey != null && OrderKey != orderKey) { OrderKey = orderKey; updated = true; }

        if (updated) UpdateTimestamp();
    }
}
