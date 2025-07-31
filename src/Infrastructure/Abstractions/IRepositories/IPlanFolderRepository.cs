using System;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface IPlanFolderRepository
{
    Task<PlanFolder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

}
