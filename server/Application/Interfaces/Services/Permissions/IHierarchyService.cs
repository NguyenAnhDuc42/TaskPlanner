using Application.Common;
using Domain.Enums;

namespace Application.Interfaces.Services.Permissions;

public interface IHierarchyService
{
    Task<HierarchyPath> ResolvePathAsync(Guid entityId, EntityType type, CancellationToken ct = default);
}
