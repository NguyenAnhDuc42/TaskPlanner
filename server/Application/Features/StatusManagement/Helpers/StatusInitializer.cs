using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Enums.RelationShip;

namespace Application.Features.StatusManagement.Helpers;

public static class StatusInitializer
{
    public static async Task InitDefaultStatuses(IUnitOfWork unitOfWork, Guid layerId, EntityLayerType layerType, Guid creatorId)
    {
        var defaultStatuses = Status.CreateDefaultStatuses(layerId, layerType, creatorId);
        await unitOfWork.Set<Status>().AddRangeAsync(defaultStatuses);
    }
}
