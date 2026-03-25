using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.DashboardFeatures.EditWidget;

public record class EditWidgetCommand(Guid dashboardId, Guid widgetId, string? configJson) : ICommand<Unit>;