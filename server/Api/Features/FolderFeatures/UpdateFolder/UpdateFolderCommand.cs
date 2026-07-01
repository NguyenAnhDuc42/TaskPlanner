using System.Text.Json.Serialization;

namespace Api;

public record UpdateFolderCommand(
    string? Name,
    string? Color,
    string? Icon,
    DateTimeOffset? StartDate,
    bool ClearStartDate,
    DateTimeOffset? DueDate,
    bool ClearDueDate,
    string? OrderKey
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid FolderId { get; set; }

    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
