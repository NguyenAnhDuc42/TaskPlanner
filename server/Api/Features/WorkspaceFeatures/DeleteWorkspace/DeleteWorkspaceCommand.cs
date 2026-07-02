using System.Text.Json.Serialization;

namespace Api;

public record DeleteWorkspaceCommand : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid WorkspaceId { get; set; }
}
