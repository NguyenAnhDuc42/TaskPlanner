namespace Domain;

public sealed class ProjectSpace : TenantEntity
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string Color { get; private set; } = "#FFFFFF";
    public string? Icon { get; private set; }
    public bool IsPrivate { get; private set; } = true;
    public bool IsArchived { get; private set; }
    public string OrderKey { get; private set; } = null!;
    public Guid DefaultDocumentId { get; private set; }

    private ProjectSpace() { }

    private ProjectSpace(Guid id, Guid projectWorkspaceId, string name, string slug, Guid defaultDocumentId, string color, string? icon, bool isPrivate, Guid creatorId, string orderKey)
        : base(id, projectWorkspaceId)
    {
        Name = name;
        Slug = slug;
        DefaultDocumentId = defaultDocumentId;
        Color = color;
        Icon = icon;
        IsPrivate = isPrivate;
        OrderKey = orderKey;
        IsArchived = false;

        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static ProjectSpace Create(Guid projectWorkspaceId, string name, string slug, Guid defaultDocumentId, string? color, string? icon, bool isPrivate, Guid creatorId, string orderKey)
    {
        var space = new ProjectSpace(
            Guid.NewGuid(), 
            projectWorkspaceId, 
            name,
            slug,
            defaultDocumentId, 
            color ?? "#FFFFFF", 
            icon,
            isPrivate, 
            creatorId, 
            orderKey);

        return space;
    }

    public static ProjectSpace CreateDefault(Guid projectWorkspaceId, Guid defaultDocumentId, Guid creatorId)
    {
        return Create(
            projectWorkspaceId,
            "Welcome Space",
            "welcome-space",
            defaultDocumentId,
            null,
            null,
            isPrivate: false,
            creatorId: creatorId,
            orderKey: FractionalIndex.Start()
        );
    }

    public void Delete()
    {
        SoftDelete();
    }

    public void Update(
        string? name = null,
        string? slug = null,
        string? color = null,
        string? icon = null,
        bool? isPrivate = null,
        string? orderKey = null)
    {
        EnsureNotArchived();
        bool updated = false;

        if (name != null && Name != name) { Name = name; updated = true; }
        if (slug != null && Slug != slug) { Slug = slug; updated = true; }
        if (color != null && Color != color) { Color = color; updated = true; }
        if (icon != null && Icon != icon) { Icon = icon; updated = true; }
        if (isPrivate != null && IsPrivate != isPrivate) { IsPrivate = isPrivate.Value; updated = true; }
        if (orderKey != null && OrderKey != orderKey) { OrderKey = orderKey; updated = true; }

        if (updated) UpdateTimestamp();
    }

    public void Archive()
    {
        if (IsArchived) return;
        IsArchived = true;
        UpdateTimestamp();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;
        IsArchived = false;
        UpdateTimestamp();
    }

    private void EnsureNotArchived()
    {
        if (IsArchived) throw new BusinessRuleException("Cannot modify an archived space.");
    }
}


