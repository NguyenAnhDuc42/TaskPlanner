using System.Text.Json.Serialization;

namespace Api;

public record UpdateSpaceCommand(
    string? Name,
    string? Color,
    string? Icon,
    bool? IsPrivate,
    string? OrderKey
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid SpaceId { get; set; }

    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
