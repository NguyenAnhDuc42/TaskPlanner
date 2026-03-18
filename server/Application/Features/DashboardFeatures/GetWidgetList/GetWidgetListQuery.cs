using Application.Common.Mediatr;
using Domain.Enums.Widget;
using MediatR;

namespace Application.Features.DashboardFeatures.GetWidgetList;

public record class GetWidgetListQuery(Guid dashboardId) : IRequest<List<WidgetDto>>;

public record WidgetDto(
    Guid Id,
    Guid DashboardId,
    WidgetLayoutDto Layout,
    WidgetType WidgetType,
    string ConfigJson,
    WidgetVisibility Visibility);

public record WidgetLayoutDto(
    int Col,
    int Row,
    int Width,
    int Height);
