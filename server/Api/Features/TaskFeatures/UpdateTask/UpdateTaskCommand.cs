using System.Text.Json.Serialization;

namespace Api;

public record UpdateTaskCommand(
    string? Name,
    string? Color,
    string? Icon,
    Guid? StatusId,
    Priority? Priority,
    DateTimeOffset? StartDate,
    bool ClearStartDate,
    DateTimeOffset? DueDate,
    bool ClearDueDate,
    int? StoryPoints,
    long? TimeEstimateSeconds,
    string? OrderKey,
    Guid? ParentTaskId
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid TaskId { get; set; }

    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
