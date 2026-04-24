using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Application.Interfaces;
using Application.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Domain.Entities;  

namespace Application.Features.WorkspaceFeatures;

public class SetWorkspacePinHandler(
    IDataBase db, 
    WorkspaceContext context,
    ICurrentUserService currentUserService, 
    HybridCache cache, 
    IRealtimeService realtime
) : ICommandHandler<SetWorkspacePinCommand>
{
    public async Task<Result> Handle(SetWorkspacePinCommand request, CancellationToken ct)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));
        
        // 1. Membership Check
        var member = context.CurrentMember;
        if (member == null || context.workspaceId != request.WorkspaceId)
        {
            member = await db.WorkspaceMembers
                .ByWorkspace(request.WorkspaceId)
                .ByUser(currentUserId)
                .WhereActive()
                .FirstOrDefaultAsync(ct);
        }

        if (member is null) 
            return Result.Failure(Error.Forbidden("Workspace.Forbidden", "Only active members can pin workspaces."));

        // 2. Logic execution
        var memberEntity = await db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.Id == member.Id, ct);

        if (memberEntity == null) return Result.Failure(MemberError.NotFound);

        memberEntity.SetPinned(request.IsPinned);
        await db.SaveChangesAsync(ct);
        
        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), ct);
        
        _ = realtime.NotifyUserAsync(currentUserId, "WorkspacePinned", new { WorkspaceId = request.WorkspaceId, IsPinned = request.IsPinned }, ct);

        return Result.Success();
    }
}
