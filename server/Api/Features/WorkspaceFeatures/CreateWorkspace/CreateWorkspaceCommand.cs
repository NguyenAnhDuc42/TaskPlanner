using System.Text.Json.Serialization;

namespace Api;

// NOT IAuthorizedWorkspaceRequest — there is no workspace yet when creating one.
// The PermissionDecorator would try to look up workspace membership and fail.
// Auth here is purely: is the HTTP request authenticated? (.RequireAuthorization() on the endpoint)
public record CreateWorkspaceCommand(
    Guid Id,
    string Name,
    string? Color,
    string? Icon,
    string? Description,
    bool? StrictJoin,
    Theme? Theme
) : ICommandRequest<Guid>
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}
