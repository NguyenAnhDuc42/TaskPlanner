using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;

namespace Application.Features.StatusManagement.Helpers;

public static class StatusInitializer
{
    public static async Task InitDefaultStatuses(IUnitOfWork unitOfWork, Guid spaceId, Guid creatorId)
    {
        var defaultStatuses = Status.CreateDefaultStatuses(spaceId, creatorId);
        await unitOfWork.Set<Status>().AddRangeAsync(defaultStatuses);
    }
}
