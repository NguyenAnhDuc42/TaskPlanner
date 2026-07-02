using System.Text.Json.Serialization;

namespace Api;

public record CreateAssigneeCommand(
    Guid Id,
    Guid TaskId,
    Guid MemberId
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
