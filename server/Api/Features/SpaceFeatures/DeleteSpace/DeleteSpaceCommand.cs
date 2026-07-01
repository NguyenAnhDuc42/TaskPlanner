using System.Text.Json.Serialization;

namespace Api;

public record DeleteSpaceCommand : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid SpaceId { get; set; }

    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
