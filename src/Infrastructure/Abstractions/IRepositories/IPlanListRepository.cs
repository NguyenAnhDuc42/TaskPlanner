using System;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface IPlanListRepository
{
    Task<PlanList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

}
