using System.Text.Json.Serialization;

namespace Api;

public record DeleteFolderCommand : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid FolderId { get; set; }

    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
