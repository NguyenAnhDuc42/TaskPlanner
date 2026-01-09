using Application.Contract.UserContract;
using Domain.Enums;
using Domain.Enums.Workspace;

namespace Application.Contract.WorkspaceContract;

public record class WorkspaceSummaryDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Description { get; init; } = null!;
    public WorkspaceVariant Variant { get; init; }

    public Role Role { get; init; }

    public int MemberCount { get; init; }
    public bool IsPinned { get; init; }
}
