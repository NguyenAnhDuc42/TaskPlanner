using System.Text.Json.Serialization;

namespace Api;

public record UpdateCommentCommand(
    string Content
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid CommentId { get; set; }

    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
