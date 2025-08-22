using System;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface IPlanTaskRepository : IBaseRepository<PlanTask>
{
    Task<PlanTask?> GetByIdAndUserAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsOwnedByUser(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
