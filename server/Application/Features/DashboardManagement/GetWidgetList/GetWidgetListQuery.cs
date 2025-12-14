using Application.Common.Interfaces;
using Application.Contract.WidgetDtos;

namespace Application.Features.DashboardManagement.GetWidgetList;

public record class GetWidgetListQuery(Guid dashboardId) : IQuery<DashboardWidgetListDto>;

