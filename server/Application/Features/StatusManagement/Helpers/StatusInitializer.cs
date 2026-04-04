using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;

namespace Application.Features.StatusManagement.Helpers;

public static class StatusInitializer
{
    public static async Task InitWorkspaceStatuses(IUnitOfWork unitOfWork, Guid workspaceId, Guid spaceId, Guid creatorId)
    {
        // 1. Create the Workflow for the Space
        var workflow = Workflow.Create(workspaceId, spaceId, "Standard Workflow", "Default space workflow", creatorId);
        await unitOfWork.Set<Workflow>().AddAsync(workflow);

        // 2. Create the Starter Set of Statuses
        var starterSet = Status.CreateStarterSet(workspaceId, workflow.Id, creatorId);
        await unitOfWork.Set<Status>().AddRangeAsync(starterSet);
    }
}
