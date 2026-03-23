using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Enums.RelationShip;

namespace Application.Features.DashboardFeatures.GetDashboardList;

public record class GetDashboardListQuery(
    Guid layerId, 
    EntityLayerType layerType, 
    CursorPaginationRequest Pagination) : IQuery<PagedResult<DashboardDto>>;

public record DashboardDto(
    Guid Id,
    string Name,
    bool IsShared,
    bool IsMain,
    EntityLayerType LayerType,
    Guid LayerId,
    DateTimeOffset UpdatedAt);