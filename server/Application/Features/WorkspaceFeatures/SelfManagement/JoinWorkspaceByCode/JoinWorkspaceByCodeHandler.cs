using System.ComponentModel.DataAnnotations;
using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Application.Features.WorkspaceFeatures.SelfManagement;
using Microsoft.Extensions.Caching.Hybrid;
using Application.Interfaces;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.JoinWorkspaceByCode;

public class JoinWorkspaceByCodeHandler : BaseFeatureHandler, IRequestHandler<JoinWorkspaceByCodeCommand, JoinWorkspaceByCodeResult>
{
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public JoinWorkspaceByCodeHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext,
        HybridCache cache,
        IRealtimeService realtime)
        : base(unitOfWork, currentUserService, workspaceContext)
    {
        _cache = cache;
        _realtime = realtime;
    }

    public async Task<JoinWorkspaceByCodeResult> Handle(JoinWorkspaceByCodeCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserId;
        var workspace = await GetWorkspaceByCode(request.JoinCode, cancellationToken);
        
        var result = await AddOrUpdateMember(workspace, currentUserId, cancellationToken);
        
        await InvalidateCache(currentUserId, cancellationToken);
        
        NotifyClients(workspace.Id, currentUserId);

        return result;
    }

    private async Task<ProjectWorkspace> GetWorkspaceByCode(string code, CancellationToken ct)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var workspace = await UnitOfWork.Set<ProjectWorkspace>()
            .FirstOrDefaultAsync(w => w.JoinCode == normalizedCode && w.DeletedAt == null, ct)
            ?? throw new ValidationException("Invalid join code.");

        if (workspace.IsArchived)
        {
            throw new ValidationException("Cannot join an archived workspace.");
        }
        return workspace;
    }

    private async Task<JoinWorkspaceByCodeResult> AddOrUpdateMember(ProjectWorkspace workspace, Guid userId, CancellationToken ct)
    {
        var existingMember = await UnitOfWork.Set<WorkspaceMember>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(wm => wm.ProjectWorkspaceId == workspace.Id && wm.UserId == userId, ct);

        JoinWorkspaceByCodeResult result;
        if (existingMember is null)
        {
            var status = workspace.StrictJoin ? MembershipStatus.Pending : MembershipStatus.Active;
            var member = WorkspaceMember.Create(userId, workspace.Id, Role.Member, status, userId, "Code");
            workspace.AddMember(member, userId);
            result = new JoinWorkspaceByCodeResult(workspace.Id, status.ToString(), true);
        }
        else if (existingMember.DeletedAt != null)
        {
            existingMember.RestoreForJoinByCode(workspace.StrictJoin);
            result = new JoinWorkspaceByCodeResult(workspace.Id, existingMember.Status.ToString(), false);
        }
        else
        {
            existingMember.JoinByCode(workspace.StrictJoin);
            result = new JoinWorkspaceByCodeResult(workspace.Id, existingMember.Status.ToString(), false);
        }

        await UnitOfWork.SaveChangesAsync(ct);
        return result;
    }

    private async Task InvalidateCache(Guid userId, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(userId), ct);
    }

    private void NotifyClients(Guid workspaceId, Guid userId)
    {
        _ = _realtime.NotifyUserAsync(userId, "WorkspaceJoined", new { WorkspaceId = workspaceId }, default);
    }
}
