using Domain.Enums.RelationShip;

namespace Domain.Entities;


public static class ViewExtensions
{
    public static IQueryable<ViewDefinition> ById(this IQueryable<ViewDefinition> query, Guid id)
        => query.Where(v => v.Id == id);

    public static IQueryable<ViewDefinition> ByLayer(this IQueryable<ViewDefinition> query, Guid layerId, EntityLayerType layerType)
        => query.Where(v => v.LayerId == layerId && v.LayerType == layerType);

    public static IQueryable<ViewDefinition> WhereDefault(this IQueryable<ViewDefinition> query)
        => query.Where(v => v.IsDefault);
}