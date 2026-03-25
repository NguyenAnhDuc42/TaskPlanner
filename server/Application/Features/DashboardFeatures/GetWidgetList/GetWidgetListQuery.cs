using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Enums.Widget;
using MediatR;
using System;
using System.Collections.Generic;

namespace Application.Features.DashboardFeatures.GetWidgetList;

public record class GetWidgetListQuery(Guid dashboardId, CursorPaginationRequest pagination) : IQuery<PagedResult<WidgetDto>>;

public record WidgetDto(
    Guid Id,
    Guid DashboardId,
    WidgetLayoutDto Layout,
    WidgetType WidgetType,
    string ConfigJson,
    DateTimeOffset UpdatedAt);

public record WidgetLayoutDto(
    int Col,
    int Row,
    int Width,
    int Height);
