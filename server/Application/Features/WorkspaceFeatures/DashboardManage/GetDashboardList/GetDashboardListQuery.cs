using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Contract.DashboardDtos;

namespace Application.Features.WorkspaceFeatures.DashboardManage.GetDashboardList;

public record class GetDashboardListQuery(CursorPaginationRequest pagination,Guid workspaceId, DashboardFilter filter, CancellationToken cancellationToken) : IQuery<PagedResult<DashboardListItemDto>>;
