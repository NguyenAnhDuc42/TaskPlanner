using Application.Common.Interfaces;
using Domain.Enums.Widget;
using MediatR;


namespace Application.Features.DashboardManagement.CreateDashboardWidget;
public record class CreateDashboardWidgetCommand(Guid dashboardId, WidgetType widgetType) : ICommand<Unit>; 