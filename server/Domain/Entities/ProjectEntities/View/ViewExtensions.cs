using Domain.Enums.RelationShip;

namespace Domain.Entities.ProjectEntities;

public static class ViewExtensions
{
    public static IQueryable<ViewDefinition> ByLayer(
        this IQueryable<ViewDefinition> query, 
        Guid layerId, 
        EntityLayerType layerType)
    {
        return query.Where(v => v.LayerId == layerId && v.LayerType == layerType);
    }
}
