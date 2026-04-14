using Application.Features;
using Application.Common.Interfaces;

namespace Application.Features.ViewFeatures.GetViewData;

public record GetViewDataQuery(
    Guid ViewId,
    int Page = 1,
    int PageSize = 100
) : IQueryRequest<ViewDataResponse>;
