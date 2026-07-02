using System.Text.Json.Serialization;

namespace Api;

public record DeleteCommentCommand : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public Guid CommentId { get; set; }

    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
