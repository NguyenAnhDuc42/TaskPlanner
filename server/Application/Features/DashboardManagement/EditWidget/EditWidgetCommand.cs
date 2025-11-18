using Application.Common.Interfaces;
using Application.Helpers.WidgetTool;
using MediatR;

namespace Application.Features.DashboardManagement.EditWidget;

public record class EditWidgetCommand(Guid dashboardId,Guid widgetId,WidgetFilter filter) : ICommand<Unit>;