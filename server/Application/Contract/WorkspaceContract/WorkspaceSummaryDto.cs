using Application.Contract.UserContract;

namespace Application.Contract.WorkspaceContract;

public record class WorkspaceSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public string Variant { get; init; } = null!;
    public bool IsOwned { get; init; }
    public List<MemberDto> Members { get; init; } = new();
}
