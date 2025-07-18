using System;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface IHierarchyRepository
{
    Task<PlanTask?> GetPlanTaskByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsOwnedByUser(Guid id, Guid userId, CancellationToken cancellationToken = default);

}   
