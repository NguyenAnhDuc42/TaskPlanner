using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application;

public class RemoveMembersHandler(TaskPlanDbContext db, RealtimeService realtimeService, PermissionService permissionService, HybridCache cache) : ICommandHandler<RemoveMembersCommand>
{
    public async Task<Result> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin,cancellationToken: cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        if (request.MemberIds.Count == 0 ) return Result.Success();

        var affected = await db.WorkspaceMembers
            .Where(wm => request.MemberIds.Contains(wm.Id) && wm.DeletedAt == null)
            .ExecuteUpdateAsync(u => u
                .SetProperty(wm => wm.DeletedAt, DateTimeOffset.UtcNow)
                .SetProperty(wm => wm.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
        
        if (affected > 0)
        {
            await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceMembersTag(request.WorkspaceId), cancellationToken);

            await realtimeService.NotifyEntitiesDeletedAsync(
                request.WorkspaceId, 
                new EntityBatchDelete { MemberIds = request.MemberIds }, 
                cancellationToken);
        } 

        return Result.Success();
    }
}


