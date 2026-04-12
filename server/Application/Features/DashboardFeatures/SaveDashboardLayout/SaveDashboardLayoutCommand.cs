using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.DashboardFeatures.SaveDashboardLayout;

[Obsolete("Dashboard features are legacy and will be removed in favor of modernized Functional Views.")]
public record SaveDashboardLayoutCommand(Guid DashboardId, List<WidgetLayoutUpdateDto> Layouts) : ICommand<bool>;

public record WidgetLayoutUpdateDto(Guid WidgetId, int Col, int Row, int Width, int Height);
