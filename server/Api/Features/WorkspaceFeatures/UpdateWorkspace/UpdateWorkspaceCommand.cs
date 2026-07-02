using System.Text.Json.Serialization;

namespace Api;

public record UpdateWorkspaceCommand(
    string? Name,
    string? Description,
    string? Color,
    string? Icon,
    bool? StrictJoin
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore] public Guid WorkspaceId { get; set; }

    [JsonIgnore] public string TraceId { get; set; } = string.Empty;
}
