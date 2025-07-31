using System;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface ISpaceRepository
{
    Task<Space?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
