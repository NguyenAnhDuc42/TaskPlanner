using Application.Contract.UserContract;

namespace Application.Contract.WorkspaceContract;

[Obsolete("Use WorkspaceSecurityContextDto for security context. UI data should be fetched separately.", false)]
public record class WorkspaceDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Description { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public string Variant { get; init; } = null!;
    public string JoinCode { get; init; } = null!;
    public bool IsOwned { get; init; }
    public MemberDto CurrentRole { get; init; } = null!;
    public List<string> Permissions { get; init; } = new();
    public WorkspaceSettingsDto Settings { get; init; } = null!;
}
