using Application.Contract.UserContract;
using Domain.Enums;

namespace Application.Contract.WorkspaceContract;

public record class WorkspaceDetail : WorkspaceSummary
{
    public Member CurrentRole { get; init; } = null!;
    public IEnumerable<Member> Members { get; init; } = Enumerable.Empty<Member>();
}