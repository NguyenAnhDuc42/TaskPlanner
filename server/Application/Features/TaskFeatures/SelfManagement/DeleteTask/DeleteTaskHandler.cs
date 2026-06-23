using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application;

public class DeleteTaskHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, PermissionService permissionService, RealtimeService realtimeService, ILogger<DeleteTaskHandler> logger) : ICommandHandler<DeleteTaskCommand>
{
    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete task {TaskId}", request.TaskId);
        
        var task = await db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId && t.DeletedAt == null, cancellationToken);
        if (task == null) 
        {
            logger.LogWarning("Task {TaskId} not found or already deleted", request.TaskId);
            return Result.Failure(TaskError.NotFound);
        }

        var memberId = workspaceContext.CurrentMember.Id;
        bool isCreator = task.CreatorId == memberId;

        // Creators (Member+) can delete their own tasks; otherwise require Admin
        var hasAccess = isCreator
            ? workspaceContext.CurrentMember.Role.IsAtLeast(Role.Member)
            : await permissionService.VerifyAsync(Role.Admin, task.ProjectSpaceId, AccessLevel.Editor, cancellationToken: cancellationToken);

        if (!hasAccess)
        {
            logger.LogWarning("Access denied for user {UserId} to delete task {TaskId}", memberId, task.Id);
            return Result.Failure(MemberError.DontHavePermission);
        }

        task.SoftDelete();
        var affectedRows = await db.SaveChangesAsync(cancellationToken);
        if (affectedRows > 0)
        {
            await realtimeService.NotifyEntitiesDeletedAsync(
                workspaceContext.WorkspaceId,
                new EntityBatchDelete { TaskIds = new List<Guid> { task.Id } },
                cancellationToken);
                
            bool hasRemainingTasks = false;
            var update = new EntityBatchUpdate();
            
            if (task.ProjectFolderId.HasValue)
            {
                hasRemainingTasks = await db.ProjectTasks.AnyAsync(t => t.ProjectFolderId == task.ProjectFolderId.Value && t.DeletedAt == null, cancellationToken);
                if (!hasRemainingTasks)
                {
                    var folder = await db.ProjectFolders.AsNoTracking().FirstOrDefaultAsync(f => f.Id == task.ProjectFolderId.Value, cancellationToken);
                    if (folder != null)
                        update.Folders = [FolderRecord.FromDomain(folder) with { HasTasks = false }];
                }
            }
            else
            {
                hasRemainingTasks = await db.ProjectTasks.AnyAsync(t => t.ProjectSpaceId == task.ProjectSpaceId && t.ProjectFolderId == null && t.DeletedAt == null, cancellationToken);
                if (!hasRemainingTasks)
                {
                    var space = await db.ProjectSpaces.AsNoTracking().FirstOrDefaultAsync(s => s.Id == task.ProjectSpaceId, cancellationToken);
                    if (space != null)
                        update.Spaces = [SpaceRecord.FromDomain(space) with { HasTasks = false }];
                }
            }

            if (update.Folders != null || update.Spaces != null)
            {
                logger.LogInformation("Broadcasting hasTasks update for parent of deleted task {TaskId}", task.Id);
                _ = realtimeService
                .NotifyEntitiesUpdatedAsync(workspaceContext.WorkspaceId, update, default)
                .ContinueWith(t =>
                    logger.LogError(t.Exception, "Failed to send real-time notification for updated parent of deleted task {TaskId}", task.Id),
                    TaskContinuationOptions.OnlyOnFaulted
                );
            }
            
            logger.LogInformation("Successfully deleted task {TaskId}", task.Id);
        }
        else
        {
            logger.LogError("Failed to save deletion of task {TaskId}", task.Id);
        }
        
        return Result.Success();
    }
}


