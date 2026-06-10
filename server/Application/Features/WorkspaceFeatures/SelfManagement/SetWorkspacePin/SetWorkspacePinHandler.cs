using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
namespace Application;

public class SetWorkspacePinHandler(
    TaskPlanDbContext db, 
    WorkspaceContext context,
    CurrentUserService currentUserService, 
    HybridCache cache, 
    RealtimeService realtime
) : ICommandHandler<SetWorkspacePinCommand>
{
    public async Task<Result> Handle(SetWorkspacePinCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));
        
        // 1. Membership Check
        var member = context.CurrentMember;
        if (member == null || context.WorkspaceId != request.WorkspaceId)
        {
            member = await db.WorkspaceMembers
                .ByWorkspace(request.WorkspaceId)
                .ByUser(currentUserId)
                .WhereActive()
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (member is null) 
            return Result.Failure(Error.Forbidden("Workspace.Forbidden", "Only active members can pin workspaces."));

        // 2. Logic execution
        var memberEntity = await db.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.Id == member.Id, cancellationToken);

        if (memberEntity == null) return Result.Failure(MemberError.NotFound);

        memberEntity.SetPinned(request.IsPinned);
        await db.SaveChangesAsync(cancellationToken);
        
        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), cancellationToken);
        
        _ = realtime.NotifyUserAsync(currentUserId, "WorkspacePinned", new { WorkspaceId = request.WorkspaceId, IsPinned = request.IsPinned }, cancellationToken);

        return Result.Success();
    }
}



