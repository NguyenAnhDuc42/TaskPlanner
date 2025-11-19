using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.DashboardManagement.MoveWidget;

public record MoveWidgetCommand(Guid dashboardId, Guid widgetId, int newCol, int newRow) : ICommand<Unit>;  
