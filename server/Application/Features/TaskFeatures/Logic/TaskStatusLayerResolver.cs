using System.ComponentModel.DataAnnotations;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TaskFeatures.Logic;

public static class TaskStatusLayerResolver
{
    public static async Task<(Guid Id, EntityLayerType Type)> GetEffectiveStatusLayer(
        IUnitOfWork unitOfWork,
        Guid entityId,
        EntityLayerType type,
        CancellationToken ct = default)
    {
        switch (type)
        {
            case EntityLayerType.ProjectList:
            {
                var list = await unitOfWork.Set<ProjectList>().AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == entityId, ct);

                if (list == null) throw new KeyNotFoundException("List not found");
                if (!list.InheritStatus) return (entityId, type);

                if (list.ProjectFolderId.HasValue)
                {
                    return await GetEffectiveStatusLayer(
                        unitOfWork,
                        list.ProjectFolderId.Value,
                        EntityLayerType.ProjectFolder,
                        ct);
                }

                return await GetEffectiveStatusLayer(
                    unitOfWork,
                    list.ProjectSpaceId,
                    EntityLayerType.ProjectSpace,
                    ct);
            }

            case EntityLayerType.ProjectFolder:
            {
                var folder = await unitOfWork.Set<ProjectFolder>().AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == entityId, ct);

                if (folder == null) throw new KeyNotFoundException("Folder not found");
                if (!folder.InheritStatus) return (entityId, type);

                return await GetEffectiveStatusLayer(
                    unitOfWork,
                    folder.ProjectSpaceId,
                    EntityLayerType.ProjectSpace,
                    ct);
            }

            case EntityLayerType.ProjectSpace:
                return (entityId, type);

            default:
                return (entityId, type);
        }
    }

    public static async Task<Guid?> ResolveTaskStatusId(
        IUnitOfWork unitOfWork,
        Guid listId,
        Guid? requestedStatusId,
        CancellationToken ct)
    {
        var (effectiveLayerId, effectiveLayerType) =
            await GetEffectiveStatusLayer(unitOfWork, listId, EntityLayerType.ProjectList, ct);

        if (requestedStatusId.HasValue)
        {
            var isValidStatus = await unitOfWork.Set<Status>().AsNoTracking()
                .AnyAsync(
                    s => s.Id == requestedStatusId.Value
                      && s.LayerId == effectiveLayerId
                      && s.LayerType == effectiveLayerType
                      && s.DeletedAt == null,
                    ct);

            if (!isValidStatus)
            {
                throw new ValidationException(
                    "Selected status does not belong to the effective status layer for this task.");
            }

            return requestedStatusId.Value;
        }

        return await unitOfWork.Set<Status>()
            .AsNoTracking()
            .Where(s =>
                s.LayerId == effectiveLayerId &&
                s.LayerType == effectiveLayerType &&
                s.DeletedAt == null)
            .OrderByDescending(s => s.IsDefaultStatus)
            .ThenBy(s => s.CreatedAt)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);
    }
}
