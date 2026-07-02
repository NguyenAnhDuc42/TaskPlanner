using System.Text.Json.Serialization;

namespace Api;

public record CreateDocumentBlockCommand(
    Guid Id,
    Guid DocumentId,
    BlockType Type,
    string Content,
    string OrderKey
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
