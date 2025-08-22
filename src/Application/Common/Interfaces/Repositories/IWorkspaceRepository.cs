using System;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface IWorkspaceRepository : IBaseRepository<Workspace>
{
    Task<Workspace?> GetWithMembersByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Workspace?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default);

}
