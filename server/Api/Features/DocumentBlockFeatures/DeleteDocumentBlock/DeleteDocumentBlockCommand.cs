using System.Text.Json.Serialization;

namespace Api;

public record DeleteDocumentBlockCommand : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid BlockId { get; set; }

    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
