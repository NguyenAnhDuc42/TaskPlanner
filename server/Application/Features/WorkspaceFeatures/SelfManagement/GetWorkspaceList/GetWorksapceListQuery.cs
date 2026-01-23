using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Contract.WorkspaceContract;
using Domain.Enums.Workspace;

namespace Application.Features.WorkspaceFeatures.GetWorkspaceList;

public record class GetWorksapceListQuery(CursorPaginationRequest Pagination, WorkspaceFilter filter) : IQuery<PagedResult<WorkspaceSummaryDto>>;
public record WorkspaceFilter(string? Name = null, bool? Owned = null, bool? isArchived = null, WorkspaceVariant? Variant = null);