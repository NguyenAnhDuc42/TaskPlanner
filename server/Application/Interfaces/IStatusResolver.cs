using Domain.Enums.RelationShip;

namespace Application.Interfaces;

public interface IStatusResolver
{
    Task<(Guid LayerId, EntityLayerType LayerType)> ResolveEffectiveLayer(Guid entityId, EntityLayerType layerType);
}
