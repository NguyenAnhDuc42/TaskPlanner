using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;


namespace Application;

public class DeleteWorkspaceHandler(
    TaskPlanDbContext db, 
    CurrentUserService currentUserService, 
    HybridCache cache, 
    RealtimeService realtime, 
    ILogger<DeleteWorkspaceHandler> logger
) : ICommandHandler<DeleteWorkspaceCommand>
{
    public async Task<Result> Handle(DeleteWorkspaceCommand request, CancellationToken ct)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        logger.LogInformation("Deleting workspace {WorkspaceId} by user {UserId}", request.workspaceId, currentUserId);

        var workspace = await db.ProjectWorkspaces
            .ById(request.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        workspace.Delete();
        await db.SaveChangesAsync(ct);
        
        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), ct);
        await realtime.NotifyUserAsync(currentUserId, "WorkspaceDeleting", new { WorkspaceId = request.workspaceId }, ct);

        return Result.Success();
    }
}



