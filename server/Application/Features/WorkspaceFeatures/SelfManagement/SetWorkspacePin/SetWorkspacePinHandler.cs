using Application.Common.Exceptions;

using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Application.Features.WorkspaceFeatures.SelfManagement;
using Microsoft.Extensions.Caching.Hybrid;
using Application.Interfaces;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.SetWorkspacePin;

public class SetWorkspacePinHandler : BaseFeatureHandler, IRequestHandler<SetWorkspacePinCommand, Unit>
{

    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public SetWorkspacePinHandler(
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

    public async Task<Unit> Handle(SetWorkspacePinCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserId;
        
        
        await UpdatePinStatus(request.WorkspaceId, request.IsPinned, currentUserId, cancellationToken);
        
        await InvalidateCache(currentUserId, cancellationToken);
        
        NotifyClients(request.WorkspaceId, currentUserId, request.IsPinned);

        return Unit.Value;
    }



    private async Task UpdatePinStatus(Guid workspaceId, bool isPinned, Guid userId, CancellationToken ct)
    {
        var member = await UnitOfWork.Set<WorkspaceMember>()
            .FirstOrDefaultAsync(wm =>
                wm.ProjectWorkspaceId == workspaceId &&
                wm.UserId == userId &&
                wm.DeletedAt == null,
                ct);

        if (member is null || member.Status != MembershipStatus.Active)
        {
            throw new ForbiddenAccessException("Only active members can pin workspaces.");
        }

        member.SetPinned(isPinned);
        await UnitOfWork.SaveChangesAsync(ct);
    }

    private async Task InvalidateCache(Guid userId, CancellationToken ct)
    {
        await _cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(userId), ct);
    }

    private void NotifyClients(Guid workspaceId, Guid userId, bool isPinned)
    {
        _ = _realtime.NotifyUserAsync(userId, "WorkspacePinned", new { WorkspaceId = workspaceId, IsPinned = isPinned }, default);
    }
}

