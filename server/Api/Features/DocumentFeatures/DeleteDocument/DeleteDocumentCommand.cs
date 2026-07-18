using System.Text.Json.Serialization;

namespace Api;

public record DeleteDocumentCommand : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid DocumentId { get; set; }

    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
