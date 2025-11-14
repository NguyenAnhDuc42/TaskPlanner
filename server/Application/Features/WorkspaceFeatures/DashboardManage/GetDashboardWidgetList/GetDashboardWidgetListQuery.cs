using Application.Common.Interfaces;
using Application.Contract.WidgetDtos;

namespace Application.Features.WorkspaceFeatures.DashboardManage.GetDashboardWidgetList;

public record class GetDashboardWidgetListQuery(Guid workspaceId,Guid? dashboardId,CancellationToken cancellationToken) : IQuery<DashboardWidgetListDto>;
