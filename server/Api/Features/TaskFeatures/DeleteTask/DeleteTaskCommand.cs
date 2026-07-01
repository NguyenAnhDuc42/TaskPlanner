using System.Text.Json.Serialization;

namespace Api;

public record DeleteTaskCommand : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid TaskId { get; set; }

    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
