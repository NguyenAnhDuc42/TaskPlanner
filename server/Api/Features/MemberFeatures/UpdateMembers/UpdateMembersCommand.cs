using System.Text.Json.Serialization;

namespace Api;

public record UpdateMembersCommand(
    List<UpdateMemberValue> Members
) : ICommandRequest<long>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}

public record UpdateMemberValue(Guid MemberId, Role? Role, MembershipStatus? Status);
