using System.Text.Json.Serialization;

namespace Api;

// IAuthorizedWorkspaceRequest: user must be a member of this workspace (validated by PermissionDecorator).
// The handler then narrows that to: only the workspace owner (CreatorId) can update it.
public record UpdateWorkspaceCommand(
    string? Name,
    string? Description,
    string? Color,
    string? Icon,
    bool? StrictJoin
) : ICommandRequest<Guid>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore] public Guid WorkspaceId { get; set; }
}
