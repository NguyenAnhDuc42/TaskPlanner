using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Enums;

namespace Application.Features.WorkspaceFeatures;

public record class GetWorksapceListQuery(CursorPaginationRequest Pagination, WorkspaceFilter filter) : IQueryRequest<PagedResult<WorkspaceSummaryDto>>;

public record WorkspaceFilter(string? Name = null, bool? Owned = null, bool? isArchived = null);

public record class WorkspaceMemberSummaryDto(Guid Id, string Name, Role Role);

public record class WorkspaceSummaryDto(
    Guid Id,
    string Name,
    string Icon,
    string Color,
    string Description,
    Role Role,
    int MemberCount,
    bool IsArchived,
    bool IsPinned,
    bool CanUpdateWorkspace,
    bool CanManageMembers,
    bool CanPinWorkspace,
    List<WorkspaceMemberSummaryDto> Members
);