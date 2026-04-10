using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Enums;
using Domain.Enums.Workspace;

namespace Application.Features.WorkspaceFeatures.SelfManagement.GetWorkspaceList;

public record class GetWorksapceListQuery(CursorPaginationRequest Pagination, WorkspaceFilter filter) : IQueryRequest<PagedResult<WorkspaceSummaryDto>>;
public record WorkspaceFilter(string? Name = null, bool? Owned = null, bool? isArchived = null);

public record class WorkspaceMemberSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public Role Role { get; init; }
}

public record class WorkspaceSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public string Color { get; init; } = null!;
    public string Description { get; init; } = null!;
    public Role Role { get; init; }
    public int MemberCount { get; init; }
    public bool IsArchived { get; init; }
    public bool IsPinned { get; init; }
    public bool CanUpdateWorkspace { get; init; }
    public bool CanManageMembers { get; init; }
    public bool CanPinWorkspace { get; init; }
    
    public List<WorkspaceMemberSummaryDto> Members { get; init; } = new();
}