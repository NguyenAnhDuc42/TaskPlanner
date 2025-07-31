using System;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface IWorkspaceRepository
{
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Workspace?> GetWithMembersByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Workspace?> GetByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);
    Task<Guid?> GetUserWorkspaceAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default);
}
