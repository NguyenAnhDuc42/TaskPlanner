using System.Text.Json.Serialization;

namespace Api;

public record CreateCommentCommand(
    Guid Id,
    Guid ProjectTaskId,
    string Content,
    Guid? ParentCommentId
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
