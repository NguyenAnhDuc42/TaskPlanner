using Domain.Common;

namespace Domain.Entities;

public class Document : TenantEntity
{
    public string Name { get; private set; } = null!;

    private Document() { }

    private Document(Guid workspaceId, string name, Guid creatorId)
        : base(Guid.NewGuid(), workspaceId)
    {
        Name = name;

        InitializeAudit(creatorId);
    }

    public static Document Create(Guid workspaceId, string name, Guid creatorId)
    {
        return new Document(workspaceId, name, creatorId);
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdateTimestamp();
    }

}
