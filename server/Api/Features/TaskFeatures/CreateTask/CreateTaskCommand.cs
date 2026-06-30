using System.Text.Json.Serialization;

namespace Api;

public record CreateTaskCommand(
    Guid Id,
    Guid DefaultDocumentId,
    Guid ProjectWorkspaceId,
    Guid? ProjectSpaceId,
    Guid? ProjectFolderId,
    string Name,
    string Slug,
    string? Color,
    string? Icon,
    Guid? StatusId,
    Priority Priority,
    string OrderKey,
    Guid? ParentTaskId
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
