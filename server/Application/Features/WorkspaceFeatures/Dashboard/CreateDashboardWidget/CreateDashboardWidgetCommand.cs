using Application.Common.Interfaces;
using Domain.Enums.Widget;
using MediatR;

namespace Application.Features.WorkspaceFeatures.Dashboard.CreateDashboardWidget;

public record class CreateDashboardWidgetCommand(Guid dashboardId,WidgetType widgetType) : ICommand<Unit>;