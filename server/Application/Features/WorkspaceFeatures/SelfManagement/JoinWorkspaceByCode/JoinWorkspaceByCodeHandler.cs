using System.ComponentModel.DataAnnotations;
using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.JoinWorkspaceByCode;

public class JoinWorkspaceByCodeHandler : ICommandHandler<JoinWorkspaceByCodeCommand, JoinWorkspaceByCodeResult>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public JoinWorkspaceByCodeHandler(IDataBase db, ICurrentUserService currentUserService, HybridCache cache, IRealtimeService realtime) {
        _db = db;
        _currentUserService = currentUserService;
        _cache = cache;
        _realtime = realtime;
    }

    public async Task<Result<JoinWorkspaceByCodeResult>> Handle(JoinWorkspaceByCodeCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) return Result.Failure<JoinWorkspaceByCodeResult>(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        var normalizedCode = request.JoinCode.Trim().ToUpperInvariant();
        var workspace = await _db.Workspaces
            .ByJoinCode(normalizedCode)
            .WhereNotDeleted()
            .FirstOrDefaultAsync(cancellationToken);
            
        if (workspace == null) return Result.Failure<JoinWorkspaceByCodeResult>(Error.Validation("Workspace.InvalidJoinCode", "Invalid join code."));
        if (workspace.IsArchived) return Result.Failure<JoinWorkspaceByCodeResult>(Error.Validation("Workspace.Archived", "Cannot join an archived workspace."));

        var existingMember = await _db.Members.IgnoreQueryFilters()
            .ByWorkspace(workspace.Id)
            .ByUser(currentUserId)
            .FirstOrDefaultAsync(cancellationToken);

        JoinWorkspaceByCodeResult dataResult;
        if (existingMember is null)
        {
            var status = workspace.StrictJoin ? MembershipStatus.Pending : MembershipStatus.Active;
            var member = WorkspaceMember.Create(currentUserId, workspace.Id, Role.Member, status, currentUserId, "Code");
            workspace.AddMember(member, currentUserId);
            dataResult = new JoinWorkspaceByCodeResult(workspace.Id, status.ToString(), true);
        }
        else if (existingMember.DeletedAt != null)
        {
            existingMember.RestoreForJoinByCode(workspace.StrictJoin);
            dataResult = new JoinWorkspaceByCodeResult(workspace.Id, existingMember.Status.ToString(), false);
        }
        else
        {
            existingMember.JoinByCode(workspace.StrictJoin);
            dataResult = new JoinWorkspaceByCodeResult(workspace.Id, existingMember.Status.ToString(), false);
        }

        await _db.SaveChangesAsync(cancellationToken);

        await _cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), cancellationToken);
        
        _ = _realtime.NotifyUserAsync(currentUserId, "WorkspaceJoined", new { WorkspaceId = workspace.Id }, default);

        return Result.Success(dataResult);
    }
}
