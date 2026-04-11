using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.SetWorkspacePin;

public class SetWorkspacePinHandler : ICommandHandler<SetWorkspacePinCommand>
{
    private readonly IDataBase _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly HybridCache _cache;
    private readonly IRealtimeService _realtime;

    public SetWorkspacePinHandler(IDataBase db, ICurrentUserService currentUserService, HybridCache cache, IRealtimeService realtime) {
        _db = db;
        _currentUserService = currentUserService;
        _cache = cache;
        _realtime = realtime;
    }

    public async Task<Result> Handle(SetWorkspacePinCommand request, CancellationToken ct)
    {
        var currentUserId = _currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));
        
        var member = await _db.Members
            .ByWorkspace(request.WorkspaceId)
            .ByUser(currentUserId)
            .WhereActive()
            .FirstOrDefaultAsync(ct);

        if (member is null) return Result.Failure(Error.Forbidden("Workspace.Forbidden", "Only active members can pin workspaces."));

        member.SetPinned(request.IsPinned);
        await _db.SaveChangesAsync(cancellationToken);
        
        await _cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), cancellationToken);
        
        _ = _realtime.NotifyUserAsync(currentUserId, "WorkspacePinned", new { WorkspaceId = request.WorkspaceId, IsPinned = request.IsPinned }, default);

        return Result.Success();
    }
}
