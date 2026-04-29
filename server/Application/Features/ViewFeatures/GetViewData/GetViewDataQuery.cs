using Application.Features;
using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.ViewFeatures;

public record GetViewDataQuery(
    Guid ViewId,
    int Page = 1,
    int PageSize = 100
) : IQueryRequest<ViewDataResponse>;

public record ViewDataResponse(
    Guid ViewId,
    ViewType ViewType,
    object Data
);
