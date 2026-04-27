using System.Linq;
using Domain.Enums.RelationShip;

namespace Domain.Entities;


public static class ViewExtensions
{
    public static IQueryable<ViewDefinition> ById(this IQueryable<ViewDefinition> query, Guid id)
        => query.Where(v => v.Id == id);

    public static IQueryable<ViewDefinition> WhereNotDeleted(this IQueryable<ViewDefinition> query) => 
        query.Where(v => v.DeletedAt == null);

    public static IQueryable<ViewDefinition> ByLayer(this IQueryable<ViewDefinition> query, Guid layerId, EntityLayerType layerType)
    {
        return layerType switch
        {
            EntityLayerType.ProjectFolder => query.Where(v => v.ProjectFolderId == layerId),
            EntityLayerType.ProjectSpace => query.Where(v => v.ProjectSpaceId == layerId),
            EntityLayerType.ProjectWorkspace => query.Where(v => v.ProjectWorkspaceId == layerId),
            _ => query.Where(v => false)
        };
    }

    public static IQueryable<ViewDefinition> WhereDefault(this IQueryable<ViewDefinition> query)
        => query.Where(v => v.IsDefault);
}