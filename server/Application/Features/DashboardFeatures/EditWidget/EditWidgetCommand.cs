using Application.Common.Interfaces;
using Domain.Enums.Widget;
using MediatR;

namespace Application.Features.DashboardFeatures.EditWidget;

public record class EditWidgetCommand(Guid dashboardId, Guid widgetId, string? configJson, WidgetVisibility? visibility) : ICommand<Unit>;