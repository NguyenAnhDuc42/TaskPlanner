using System;
using src.Domain.Entities.WorkspaceEntity;

namespace src.Infrastructure.Abstractions.IRepositories;

public interface IHierarchyRepository
{
    Task<PlanTask?> GetPlanTaskByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsOwnedByUser(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<Workspace?> GetWorkspaceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Workspace?> GetWorkspaceWithMembersByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Workspace?> GetWorkspaceByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);
    Task<Guid?> GetUserWorkspaceAsync(Guid userId, Guid workspaceId, CancellationToken cancellationToken = default);
    Task<Space?> GetSpaceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PlanTask?> GetPlanTaskByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<PlanList?> GetPlanListByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PlanFolder?> GetPlanFolderByIdAsync(Guid id, CancellationToken cancellationToken = default);

}   
