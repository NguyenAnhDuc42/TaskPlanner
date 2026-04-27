using Domain.Common;

namespace Domain.Entities;

public class Document : TenantEntity
{
    public string Name { get; private set; } = null!;
    public string Content { get; private set; } = null!;

    private Document() { }

    private Document(Guid workspaceId, string name, string content, Guid creatorId)
        : base(Guid.NewGuid(), workspaceId)
    {
        Name = name;
        Content = content;
        
        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static Document Create(Guid workspaceId, string name, string content, Guid creatorId)
    {
        return new Document(workspaceId, name, content, creatorId);
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdateTimestamp();
    }

    public void UpdateContent(string content)
    {
        Content = content;
        UpdateTimestamp();
    }
}
