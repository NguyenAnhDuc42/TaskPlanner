using System;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface IWorkspaceRepository
{
    Task<Workspace> GetWorkspaceByIdAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Workspace>> GetListOfWorkspace(Guid userId, CancellationToken cancellationToken = default);
    
}
