using Application.Common.Interfaces;
using Domain.Enums.Widget;
using MediatR;

namespace Application.Features.DashboardFeatures.CreateWidget;

public record class CreateWidgetCommand(Guid dashboardId, WidgetType widgetType, int Col, int Row, int Width, int Height) : ICommand<Unit>;
