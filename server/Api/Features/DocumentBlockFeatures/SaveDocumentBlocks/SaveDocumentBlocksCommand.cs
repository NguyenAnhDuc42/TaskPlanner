using System.Text.Json.Serialization;

namespace Api;

public record BlockSaveItem(Guid Id, BlockType Type, string Content, string OrderKey, bool IsDeleted);

public record SaveDocumentBlocksCommand(
    Guid DocumentId,
    List<BlockSaveItem> Blocks
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
