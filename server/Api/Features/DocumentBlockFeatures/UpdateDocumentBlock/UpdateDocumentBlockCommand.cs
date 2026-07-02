using System.Text.Json.Serialization;

namespace Api;

public record UpdateDocumentBlockCommand(
    string? Content,
    string? OrderKey,
    BlockType? Type
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid BlockId { get; set; }

    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
