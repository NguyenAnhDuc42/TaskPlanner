using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TaskFeatures.Logic;

public static class TaskStatusSemanticMapper
{
    public static async Task<Guid?> MapRequestedStatusToEffectiveLayer(
        IUnitOfWork unitOfWork,
        Guid effectiveLayerId,
        EntityLayerType effectiveLayerType,
        Guid? requestedStatusId,
        CancellationToken ct)
    {
        if (!requestedStatusId.HasValue) return null;

        var requested = await unitOfWork.Set<Status>()
            .AsNoTracking()
            .Where(s => s.Id == requestedStatusId.Value && s.DeletedAt == null)
            .Select(s => new { s.Name, s.Category })
            .FirstOrDefaultAsync(ct);

        if (requested == null) return requestedStatusId;

        var exactExists = await unitOfWork.Set<Status>()
            .AsNoTracking()
            .AnyAsync(s =>
                s.Id == requestedStatusId.Value &&
                s.LayerId == effectiveLayerId &&
                s.LayerType == effectiveLayerType &&
                s.DeletedAt == null,
                ct);

        if (exactExists) return requestedStatusId;

        var semanticMatch = await unitOfWork.Set<Status>()
            .AsNoTracking()
            .Where(s =>
                s.LayerId == effectiveLayerId &&
                s.LayerType == effectiveLayerType &&
                s.DeletedAt == null &&
                s.Category == requested.Category &&
                s.Name.ToLower() == requested.Name.ToLower())
            .OrderByDescending(s => s.IsDefaultStatus)
            .ThenBy(s => s.CreatedAt)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);

        return semanticMatch ?? requestedStatusId;
    }
}
