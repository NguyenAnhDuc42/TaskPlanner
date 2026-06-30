namespace Domain;

public class Document : TenantEntity
{
    public string Name { get; private set; } = null!;

    private Document() { }

    private Document(Guid id, Guid workspaceId, string name, Guid creatorId)
        : base(id, workspaceId)
    {
        Name = name;

        InitializeAudit(creatorId);
    }

    public static Document Create(Guid workspaceId, string name, Guid creatorId)
    {
        return new Document(Guid.NewGuid(), workspaceId, name, creatorId);
    }

    public static Document Create(Guid id, Guid workspaceId, string name, Guid creatorId)
    {
        return new Document(id, workspaceId, name, creatorId);
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdateTimestamp();
    }

}


