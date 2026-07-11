using System.Text.Json.Serialization;

namespace Api;

// Batch-shaped like CreateSpace's multi-entity fan-out: one call can create/update/delete
// several statuses in one round-trip, each row tagged with its own RowAction. Scope is the
// workspace (from IAuthorizedWorkspaceRequest), not a single space — status is workspace-visible
// everywhere; SpaceId per row is an optional "ancestor" tag, not a scoping parameter.
public record UpdateWorkspaceStatusesCommand(
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
    string? OrderKey,
    Guid? SpaceId,
    RowAction Action
);
