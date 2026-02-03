using Domain.Enums.RelationShip;
using Domain.Permission;

namespace Application.Interfaces.Services.Permissions;

public interface IResourceHierarchyProvider
{
    Task<EntityPath> GetPathToRoot(
        Guid entityId,
        EntityLayerType entityLayer,
        CancellationToken ct);
}
