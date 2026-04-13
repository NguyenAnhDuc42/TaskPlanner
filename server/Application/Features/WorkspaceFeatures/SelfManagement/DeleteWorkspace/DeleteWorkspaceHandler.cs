using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features;
using Application.Features.WorkspaceFeatures.DeleteWorkspace;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;


namespace Application.Features.WorkspaceFeatures.SelfManagement.DeleteWorkspace;

public class DeleteWorkspaceHandler(
    IDataBase db, 
    ICurrentUserService currentUserService, 
    HybridCache cache, 
    IRealtimeService realtime, 
    ILogger<DeleteWorkspaceHandler> logger,
    IBackgroundJobService backgroundJob
) : ICommandHandler<DeleteWorkspaceCommand>
{
    public async Task<Result> Handle(DeleteWorkspaceCommand request, CancellationToken ct)
    {
        var currentUserId = currentUserService.CurrentUserId();
        if (currentUserId == Guid.Empty) 
            return Result.Failure(Error.Unauthorized("User.NotAuthenticated", "User not authenticated."));

        logger.LogInformation("Deleting workspace {WorkspaceId} by user {UserId}", request.workspaceId, currentUserId);

        var workspace = await db.Workspaces
            .ById(request.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        // 1. Logic: Use formal domain method to trigger background cleanup
        workspace.Delete();
        await db.SaveChangesAsync(ct);

        // 2. Instant Trigger for Background Cleanup
        backgroundJob.TriggerOutbox();

        // --- Side Effects ---
        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(currentUserId), ct);
        
        // 3. STAGE 1 Notification: UI hides the workspace immediately while cleanup starts
        await realtime.NotifyUserAsync(currentUserId, "WorkspaceDeleting", new { WorkspaceId = request.workspaceId }, ct);

        return Result.Success();
    }
}
