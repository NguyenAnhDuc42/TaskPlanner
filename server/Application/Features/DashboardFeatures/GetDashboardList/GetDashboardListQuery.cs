using Application.Common.Mediatr;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.DashboardFeatures.GetDashboardList;

public record class GetDashboardListQuery(Guid layerId, EntityLayerType layerType) : IRequest<List<DashboardDto>>;

public record DashboardDto(
    Guid Id,
    string Name,
    bool IsShared,
    bool IsMain,
    EntityLayerType LayerType,
    Guid LayerId);