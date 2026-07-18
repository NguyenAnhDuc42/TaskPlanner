using System.Text.Json.Serialization;

namespace Api;

public record CreateDocumentCommand(
    Guid Id,
    Guid SpaceId,
    Guid? ParentDocumentId,
    string Name,
    string? Icon,
    string? Color
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
