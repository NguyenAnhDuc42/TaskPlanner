using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application;

public class UpdateFolderHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, PermissionService permissionService, RealtimeService realtimeService, ILogger<UpdateFolderHandler> logger) : ICommandHandler<UpdateFolderCommand>
{
    public async Task<Result> Handle(UpdateFolderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to update folder {FolderId}", request.FolderId);
        
        var folder = await db.ProjectFolders.FirstOrDefaultAsync(f => f.Id == request.FolderId && f.DeletedAt == null, cancellationToken);
        if (folder == null) 
        {
            logger.LogWarning("Folder {FolderId} not found or deleted", request.FolderId);
            return Result.Failure(FolderError.NotFound);
        }

        var hasAccess = await permissionService.VerifyAsync(Role.Member, folder.ProjectSpaceId, AccessLevel.Editor, folder.CreatorId, cancellationToken);
        if (!hasAccess) 
        {
            logger.LogWarning("Access denied for user {UserId} to update folder {FolderId}", workspaceContext.CurrentMember.Id, folder.Id);
            return Result.Failure(MemberError.DontHavePermission);
        }

        
        folder.Update(
            name: request.Name,
            slug: request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null,
            color: request.Color,
            icon: request.Icon,
            startDate: request.StartDate,
            dueDate: request.DueDate,
            statusId: request.StatusId,
            priority: request.Priority,
            clearStartDate: request.ClearStartDate,
            clearDueDate: request.ClearDueDate,
            clearStatusId: request.ClearStatusId,
            clearPriority: request.ClearPriority
        );

        var affectedRows = await db.SaveChangesAsync(cancellationToken);
        if (affectedRows > 0)
        {
            logger.LogInformation("Broadcasting entity updates for updated folder {FolderId}", folder.Id);
            _ = realtimeService.NotifyEntitiesUpdatedAsync(
                workspaceContext.TryGetWorkspaceId().Value,
                new EntityBatchUpdate { Folders = [FolderRecord.FromDomain(folder, workflowId: null)] },
                default)
            .ContinueWith(t =>
                logger.LogError(t.Exception, "Failed to send real-time notification for updated folder {FolderId}", folder.Id),
                TaskContinuationOptions.OnlyOnFaulted);

            logger.LogInformation("Successfully updated folder {FolderId}", folder.Id);
        }
        else
        {
            logger.LogError("Failed to save updates for folder {FolderId}", folder.Id);
        }

        return Result.Success();
    }
}



