using System.Text.Json.Serialization;

namespace Api;

public record RemoveMembersCommand(
    List<Guid> MemberIds
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
