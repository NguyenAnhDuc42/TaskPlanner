using System.Text.Json.Serialization;

namespace Api;

public record CreateSpaceCommand(
    Guid Id,
    Guid DefaultDocumentId,
    string Name,
    string? Color,
    string? Icon,
    bool IsPrivate
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
