using Application.Common.Exceptions;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.WorkspaceFeatures.Logic;

public class WorkspacePermissionLogic
{
    private readonly IUnitOfWork _unitOfWork;

    public WorkspacePermissionLogic(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkspacePermissionSnapshot> GetSnapshot(
        Guid workspaceId,
        Guid userId,
        CancellationToken ct)
    {
        var workspace = await _unitOfWork.Set<ProjectWorkspace>()
            .AsNoTracking()
            .Where(w => w.Id == workspaceId && w.DeletedAt == null)
            .Select(w => new { w.Id, w.CreatorId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Workspace {workspaceId} not found.");

        var member = await _unitOfWork.Set<WorkspaceMember>()
            .AsNoTracking()
            .Where(wm =>
                wm.ProjectWorkspaceId == workspaceId &&
                wm.UserId == userId &&
                wm.DeletedAt == null)
            .Select(wm => new { wm.Role, wm.Status })
            .FirstOrDefaultAsync(ct);

        var isOwned = workspace.CreatorId.HasValue && workspace.CreatorId.Value == userId;
        var isMember = member is not null;
        var isSuspended = member?.Status == MembershipStatus.Suspended;
        var role = isOwned ? Role.Owner : member?.Role ?? Role.None;

        var canViewHierarchy = isOwned || (isMember && !isSuspended && member!.Status == MembershipStatus.Active);
        var canManageMembers = canViewHierarchy && (role == Role.Owner || role == Role.Admin);
        var canUpdateWorkspace = canViewHierarchy && (role == Role.Owner || role == Role.Admin);
        var canDeleteWorkspace = isOwned;
        var canCreateSpace = canViewHierarchy && role != Role.Guest && role != Role.None;

        return new WorkspacePermissionSnapshot
        {
            WorkspaceId = workspaceId,
            Role = role,
            IsOwned = isOwned,
            IsMember = isMember,
            IsSuspended = isSuspended,
            CanViewHierarchy = canViewHierarchy,
            CanManageWorkspace = canUpdateWorkspace,
            CanManageMembers = canManageMembers,
            CanCreateSpace = canCreateSpace,
            CanUpdateWorkspace = canUpdateWorkspace,
            CanDeleteWorkspace = canDeleteWorkspace
        };
    }

    public async Task EnsureCanViewHierarchy(Guid workspaceId, Guid userId, CancellationToken ct)
    {
        var snapshot = await GetSnapshot(workspaceId, userId, ct);
        if (!snapshot.CanViewHierarchy)
        {
            throw new ForbiddenAccessException("You do not have permission to view this workspace hierarchy.");
        }
    }

    public async Task EnsureCanManageMembers(Guid workspaceId, Guid userId, CancellationToken ct)
    {
        var snapshot = await GetSnapshot(workspaceId, userId, ct);
        if (!snapshot.CanManageMembers)
        {
            throw new ForbiddenAccessException("You do not have permission to manage members in this workspace.");
        }
    }

    public async Task EnsureCanUpdateWorkspace(Guid workspaceId, Guid userId, CancellationToken ct)
    {
        var snapshot = await GetSnapshot(workspaceId, userId, ct);
        if (!snapshot.CanUpdateWorkspace)
        {
            throw new ForbiddenAccessException("You do not have permission to update this workspace.");
        }
    }

    public async Task EnsureCanDeleteWorkspace(Guid workspaceId, Guid userId, CancellationToken ct)
    {
        var snapshot = await GetSnapshot(workspaceId, userId, ct);
        if (!snapshot.CanDeleteWorkspace)
        {
            throw new ForbiddenAccessException("You do not have permission to delete this workspace.");
        }
    }
}

