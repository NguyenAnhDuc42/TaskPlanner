namespace Domain;

public sealed class Document : TenantEntity
{
    public Guid ProjectSpaceId { get; private set; }
    public Guid? ParentDocumentId { get; private set; }
    public string Name { get; private set; } = null!;
    public string OrderKey { get; private set; } = null!;
    public string Icon { get; private set; } = "FileText";
    public string Color { get; private set; } = "#ffffff";

    private Document() { }

    private Document(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, Guid? parentDocumentId, string name, string orderKey, Guid creatorId, string icon, string color)
        : base(id, projectWorkspaceId)
    {
        ProjectSpaceId = projectSpaceId;
        ParentDocumentId = parentDocumentId;
        Name = name;
        OrderKey = orderKey;
        Icon = icon;
        Color = color;
        InitializeAudit(creatorId);
    }

    public static Document Create(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, string name, string orderKey, Guid creatorId, Guid? parentDocumentId = null, string? icon = null, string? color = null)
    {
        if (id == Guid.Empty) throw new BusinessRuleException("Id cannot be empty.");
        return new Document(id, projectWorkspaceId, projectSpaceId, parentDocumentId, name, orderKey, creatorId, icon ?? "FileText", color ?? "#ffffff");
    }

    public void Delete() => SoftDelete();

    public void Update(string? name = null, Guid? parentDocumentId = null, bool clearParent = false, string? orderKey = null, string? icon = null, string? color = null)
    {
        bool updated = false;

        if (name != null && Name != name) { Name = name; updated = true; }

        if (clearParent && ParentDocumentId != null) { ParentDocumentId = null; updated = true; }
        else if (parentDocumentId.HasValue && ParentDocumentId != parentDocumentId) { ParentDocumentId = parentDocumentId; updated = true; }

        if (orderKey != null && OrderKey != orderKey) { OrderKey = orderKey; updated = true; }
        if (icon != null && Icon != icon) { Icon = icon; updated = true; }
        if (color != null && Color != color) { Color = color; updated = true; }

        if (updated) UpdateTimestamp();
    }
}
