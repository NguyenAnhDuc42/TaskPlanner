namespace Domain;

public class Status : TenantEntity
{
    // Nullable — Status is workspace-visible everywhere by default (WorkspaceId, from
    // TenantEntity, is the real scope). ProjectSpaceId is an optional "ancestor" tag, the same
    // relationship Task/Folder already have with their own ancestor ids: a filter dimension
    // ("this space's own statuses"), not an exclusive scope. A status created via the
    // workspace-wide Workflow Manager may have no space tag at all.
    public Guid? ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public string OrderKey { get; private set; } = null!;

    private Status() { } // EF Core

    private Status(Guid id, Guid projectWorkspaceId, Guid? projectSpaceId, string name, string color, Guid creatorId, string orderKey)
        : base(id, projectWorkspaceId)
    {
        ProjectSpaceId = projectSpaceId;
        Name = name;
        Color = color;
        OrderKey = orderKey;
        InitializeAudit(creatorId);
    }

    public static Status Create(Guid projectWorkspaceId, Guid? projectSpaceId, string name, string color, Guid creatorId, string? orderKey = null, Guid? id = null)
        => new Status(id ?? Guid.NewGuid(), projectWorkspaceId, projectSpaceId, name, color, creatorId, orderKey ?? FractionalIndex.Start());

    // Only called once, when a workspace's very first space is seeded at workspace creation
    // (see WorkspaceService) — every space created after that reuses the workspace's existing
    // status pool instead of seeding its own, since statuses are workspace-visible everywhere.
    public static List<Status> CreateSpaceStarterSet(Guid projectWorkspaceId, Guid projectSpaceId, Guid creatorId)
    {
        var start = FractionalIndex.Start();
        var key2 = FractionalIndex.After(start);
        var key3 = FractionalIndex.After(key2);
        var key4 = FractionalIndex.After(key3);

        return new List<Status>
        {
            new Status(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, "Planned",     "#808080", creatorId, start),
            new Status(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, "In Progress", "#1e90ff", creatorId, key2),
            new Status(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, "Paused",      "#ff8c00", creatorId, key3),
            new Status(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, "Completed",   "#008000", creatorId, key4)
        };
    }

    public void Update(
        string? name = null,
        string? color = null,
        string? orderKey = null)
    {
        bool updated = false;

        if (name != null && Name != name) { Name = name; updated = true; }
        if (color != null && Color != color) { Color = color; updated = true; }
        if (orderKey != null && OrderKey != orderKey) { OrderKey = orderKey; updated = true; }

        if (updated) UpdateTimestamp();
    }
}
