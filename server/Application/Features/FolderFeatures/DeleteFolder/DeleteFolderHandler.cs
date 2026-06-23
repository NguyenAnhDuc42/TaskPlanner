using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application;

public class DeleteFolderHandler(
    TaskPlanDbContext db, 
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    ILogger<DeleteFolderHandler> logger
) : ICommandHandler<DeleteFolderCommand>
{
    public async Task<Result> Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete folder {FolderId}", request.FolderId);
        var folder = await db.ProjectFolders.FirstOrDefaultAsync(f => f.Id == request.FolderId, cancellationToken);
        if (folder == null) 
        {
            logger.LogWarning("Folder {FolderId} not found or already deleted", request.FolderId);
            return Result.Failure(FolderError.NotFound);
        }

        var hasAccess = await permissionService.VerifyAsync(Role.Admin, folder.ProjectSpaceId, AccessLevel.Editor, folder.CreatorId, cancellationToken);
        if (!hasAccess) 
        {
            logger.LogWarning("Access denied for user {UserId} to delete folder {FolderId}", workspaceContext.CurrentMember.Id, request.FolderId);
            return Result.Failure(MemberError.DontHavePermission);
        }

        folder.Delete();

        var affectedRows = await db.SaveChangesAsync(cancellationToken);
        if (affectedRows > 0)
        {
            await realtimeService.NotifyEntitiesDeletedAsync(
                workspaceContext.TryGetWorkspaceId().Value,
                new EntityBatchDelete { FolderIds = [folder.Id] },
                cancellationToken);
                
            bool hasRemainingFolders = await db.ProjectFolders.AnyAsync(f => f.ProjectSpaceId == folder.ProjectSpaceId && f.DeletedAt == null, cancellationToken);
            if (!hasRemainingFolders)
            {
                var space = await db.ProjectSpaces.AsNoTracking().FirstOrDefaultAsync(s => s.Id == folder.ProjectSpaceId, cancellationToken);
                if (space != null)
                {
                    var spaceRecord = SpaceRecord.FromDomain(space) with { HasFolders = false };
                    
                    logger.LogInformation("Broadcasting hasFolders=false update for space {SpaceId} since last folder was deleted", space.Id);
                    _ = realtimeService
                    .NotifyEntitiesUpdatedAsync(
                        workspaceContext.TryGetWorkspaceId().Value,
                        new EntityBatchUpdate { Spaces = [spaceRecord] },
                        default)
                    .ContinueWith(t => 
                        logger.LogError(t.Exception, "Failed to send real-time notification for updated space {SpaceId} after folder deletion", space.Id), 
                        TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            logger.LogInformation("Successfully deleted folder {FolderId}", folder.Id);
        }
        else
        {
            logger.LogError("Failed to save deletion of folder {FolderId}", folder.Id);
        }
 
        return Result.Success();
    }
}



