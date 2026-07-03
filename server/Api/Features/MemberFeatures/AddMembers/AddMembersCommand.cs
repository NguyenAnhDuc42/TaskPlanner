using System.Text.Json.Serialization;
using Application;

namespace Api;

public record AddMembersCommand(
    List<AddMemberValue> Members
) : ICommandRequest<AddMembersResult>, IAuthorizedWorkspaceRequest
{
    [JsonIgnore]
    public string TraceId { get; set; } = string.Empty;
}

public record AddMemberValue(string Email, Role Role);

// Members is the accepted-and-created subset — the caller can't generate WorkspaceMember ids
// client-side (they depend on resolving email -> an existing user server-side), unlike Task/
// Space/Folder create, so the response has to hand back full records for the client to adopt.
// Also: GroupExcept means the adding client's own connection never receives the resulting Delta,
// so this response is the ONLY way that client's local store learns of the new members at all.
public record AddMembersResult(long SyncEventId, List<MemberRecord> Members);
