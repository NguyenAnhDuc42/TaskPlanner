using System.Text.Json.Serialization;

namespace Api;

public record CreateFolderCommand(
    Guid Id,
    Guid SpaceId,
    string Name,
    string? Color,
    string? Icon,
    DateTimeOffset? StartDate,
    DateTimeOffset? DueDate
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
