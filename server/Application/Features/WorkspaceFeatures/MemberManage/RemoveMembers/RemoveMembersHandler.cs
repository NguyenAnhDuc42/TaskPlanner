using Microsoft.EntityFrameworkCore;

namespace Application;

public class RemoveMembersHandler(TaskPlanDbContext db, WorkspaceContext context, RealtimeService realtimeService, PermissionService permissionService) : ICommandHandler<RemoveMembersCommand>
{
    public async Task<Result> Handle(RemoveMembersCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin,cancellationToken: cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        if (request.MemberIds.Count == 0 ) return Result.Success();

        var affected = await db.WorkspaceMembers
            .Where(wm => wm.ProjectWorkspaceId == request.WorkspaceId 
                        && request.MemberIds.Contains(wm.UserId) 
                        && wm.DeletedAt == null)
            .ExecuteUpdateAsync(u => u
                .SetProperty(wm => wm.DeletedAt, DateTimeOffset.UtcNow)
                .SetProperty(wm => wm.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);
        
        if (affected > 0){
            await realtimeService.NotifyEntitiesDeletedAsync(
                request.WorkspaceId, 
                new EntityBatchDelete { MemberIds = request.MemberIds }, 
                cancellationToken);
        } 

        return Result.Success();
    }
}


