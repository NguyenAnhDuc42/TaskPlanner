namespace Domain;

public class DocumentBlock : TenantEntity
{
    public Guid DocumentId { get; private set; }
    public BlockType Type { get; private set; }
    public string Content { get; private set; } = null!;
    public string OrderKey { get; private set; } = null!;

    private DocumentBlock() { }

    private DocumentBlock(Guid id, Guid workspaceId, Guid documentId, BlockType type, string content, string orderKey, Guid creatorId)
        : base(id, workspaceId)
    {
        DocumentId = documentId;
        Type = type;
        Content = content;
        OrderKey = orderKey;

        InitializeAudit(creatorId);
    }

    public static DocumentBlock Create(Guid workspaceId, Guid documentId, BlockType type, string content, string orderKey, Guid creatorId)
        => new(Guid.NewGuid(), workspaceId, documentId, type, content, orderKey, creatorId);

    public static DocumentBlock CreateWithId(Guid id, Guid workspaceId, Guid documentId, BlockType type, string content, string orderKey, Guid creatorId)
        => new(id, workspaceId, documentId, type, content, orderKey, creatorId);

    public void UpdateContent(string content)
    {
        Content = content;
        UpdateTimestamp();
    }

    public void UpdateType(BlockType type)
    {
        Type = type;
        UpdateTimestamp();
    }

    public void UpdateOrderKey(string orderKey)
    {
        OrderKey = orderKey;
        UpdateTimestamp();
    }
}


