using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class StatusResolver : IStatusResolver
{
    private readonly IUnitOfWork _unitOfWork;

    public StatusResolver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<(Guid LayerId, EntityLayerType LayerType)> ResolveEffectiveLayer(Guid entityId, EntityLayerType layerType)
    {
        switch (layerType)
        {
            case EntityLayerType.ProjectList:
                var list = await _unitOfWork.Set<ProjectList>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == entityId);
                if (list == null) throw new KeyNotFoundException("List not found");
                if (!list.InheritStatus) return (entityId, layerType);
                
                if (list.ProjectFolderId.HasValue)
                    return await ResolveEffectiveLayer(list.ProjectFolderId.Value, EntityLayerType.ProjectFolder);
                
                return await ResolveEffectiveLayer(list.ProjectSpaceId, EntityLayerType.ProjectSpace);

            case EntityLayerType.ProjectFolder:
                var folder = await _unitOfWork.Set<ProjectFolder>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == entityId);
                if (folder == null) throw new KeyNotFoundException("Folder not found");
                if (!folder.InheritStatus) return (entityId, layerType);

                return await ResolveEffectiveLayer(folder.ProjectSpaceId, EntityLayerType.ProjectSpace);

            case EntityLayerType.ProjectSpace:
                // Space is top level (or Workspace level if we support workspace-wide statuses later)
                return (entityId, layerType);

            default:
                return (entityId, layerType);
        }
    }
}
