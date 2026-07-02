using System.Text.Json.Serialization;

namespace Api;

public record DeleteAssigneeCommand : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid AssigneeId { get; set; }

    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
