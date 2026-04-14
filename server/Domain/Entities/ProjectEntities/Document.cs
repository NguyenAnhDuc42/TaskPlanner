using Domain.Common;

namespace Domain.Entities;

public class Document : TenantEntity
{
    public string Name { get; private set; } = null!;
    public string Content { get; private set; } = string.Empty;

    private Document() { }

    private Document(Guid workspaceId, string name, string content, Guid creatorId)
        : base(Guid.NewGuid(), workspaceId)
    {
        Name = name;
        Content = content;
        CreatorId = creatorId;
    }

    public static Document Create(Guid workspaceId, string name, string content, Guid creatorId)
    {
        return new Document(workspaceId, name, content, creatorId);
    }

    public void Update(string name, string content)
    {
        Name = name;
        Content = content;
        UpdateTimestamp();
    }
}
