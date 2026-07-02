using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api;

public record BatchFlushItem(
    string TraceId,
    SyncEntityType EntityType,
    SyncAction Action,
    Guid EntityId,
    JsonElement? Data
);

public record BatchFlushItemResult(string TraceId, bool Success, string? Error);
public record BatchFlushResult(List<BatchFlushItemResult> Results);

public record BatchFlushCommand(List<BatchFlushItem> Items) : ICommandRequest<BatchFlushResult>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
