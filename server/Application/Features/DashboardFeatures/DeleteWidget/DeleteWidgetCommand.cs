using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.DashboardFeatures.DeleteWidget;

public record class DeleteWidgetCommand(Guid dashboardId, Guid widgetId) : ICommand<Unit>;
