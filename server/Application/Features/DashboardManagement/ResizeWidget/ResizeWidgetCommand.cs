using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.DashboardManagement.ResizeWidget;

public record ResizeWidgetCommand(Guid dashboardId, Guid widgetId, int newWidth, int newHeight) : ICommand<Unit>;

