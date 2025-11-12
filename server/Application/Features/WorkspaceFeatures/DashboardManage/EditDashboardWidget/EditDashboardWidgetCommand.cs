using Application.Common.Interfaces;
using Application.Helpers.WidgetTool;
using MediatR;

namespace Application.Features.WorkspaceFeatures.DashboardManage.EditDashboardWidget;

public record class EditDashboardWidgetCommand(Guid dashboardId,Guid widgetId,WidgetFilter filter) : ICommand<Unit>;
