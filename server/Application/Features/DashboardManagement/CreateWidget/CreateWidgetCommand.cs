using Application.Common.Interfaces;
using Domain.Enums.Widget;
using MediatR;

namespace Application.Features.DashboardManagement.CreateWidget;

public record class CreateWidgetCommand(Guid dashboardId, WidgetType widgetType) : ICommand<Unit>; 
