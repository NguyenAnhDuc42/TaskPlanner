using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.DashboardManage.EditDashboardWidgetGrid;

public record class EditDashboardWidgetGridCommand(Guid dashboardId,List<WidgetGridUpdateItem> updateItems ,CancellationToken cancellationToken) : ICommand<Unit>;

public record WidgetGridUpdateItem(
    Guid DashboardWidgetId,
    int NewCol,
    int NewRow,
    int NewWidth,
    int NewHeight
);