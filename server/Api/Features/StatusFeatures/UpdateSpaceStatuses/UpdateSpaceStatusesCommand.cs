using System.Text.Json.Serialization;

namespace Api;

// Batch-shaped like CreateSpace's multi-entity fan-out: one call can create/update/delete
// several statuses for a space in one round-trip, each row tagged with its own RowAction.
public record UpdateSpaceStatusesCommand(
    Guid SpaceId,
    List<StatusUpdateValue> Statuses
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}

public record StatusUpdateValue(
    Guid? Id,
    string Name,
    string Color,
    StatusCategory Category,
    string? OrderKey,
    RowAction Action
);
