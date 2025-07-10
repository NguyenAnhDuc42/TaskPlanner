    using System;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface ISpaceRepository
{
    Task<IEnumerable<Space>> GetSpacesByWorkspaceIdAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<Space> GetSpaceByIdAsync(Guid spaceId, CancellationToken cancellationToken = default);
}
