using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using Domain.Enums;

namespace Application.Features.ViewFeatures.GetViews;

public record GetViewsQuery(Guid LayerId, EntityLayerType LayerType) : IQuery<List<ViewDto>>;

public record ViewDto(
    Guid Id,
    string Name,
    ViewType ViewType,
    bool IsDefault
);
