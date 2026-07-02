using System.Text.Json.Serialization;

namespace Api;

// Batch-shaped like Status: one call grants/updates/revokes several members'
// access to a space in one round-trip, each row tagged with its own RowAction.
public record EntityAccessBatchCommand(
    Guid SpaceId,
    List<EntityAccessRowsValue> Rows
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}

public record EntityAccessRowsValue(Guid? Id, Guid MemberId, AccessLevel AccessLevel, RowAction Action);
