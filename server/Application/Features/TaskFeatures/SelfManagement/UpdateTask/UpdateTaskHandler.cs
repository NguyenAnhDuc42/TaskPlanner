using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Application;

public class UpdateTaskHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, PermissionService permissionService, RealtimeService realtimeService, ILogger<UpdateTaskHandler> logger) : ICommandHandler<UpdateTaskCommand>
{
    public async Task<Result> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to update task {TaskId}", request.TaskId);
        
        var task = await db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId && t.DeletedAt == null, cancellationToken);
        if (task == null) 
        {
            logger.LogWarning("Task {TaskId} not found or deleted", request.TaskId);
            return Result.Failure(TaskError.NotFound);
        }

        var hasAccess = await permissionService.VerifyAsync(Role.Member, task.ProjectSpaceId, AccessLevel.Editor, task.CreatorId, cancellationToken);
        if (!hasAccess) 
        {
            logger.LogWarning("Access denied for user {UserId} to update task {TaskId}", workspaceContext.CurrentMember.Id, task.Id);
            return Result.Failure(MemberError.DontHavePermission);
        }

        var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;

        task.Update(
            name: request.Name,
            slug: slug,
            color: request.Color,
            icon: request.Icon,
            priority: request.Priority,
            startDate: request.StartDate,
            clearStartDate: request.ClearStartDate,
            dueDate: request.DueDate,
            clearDueDate: request.ClearDueDate,
            storyPoints: request.StoryPoints,
            timeEstimateSeconds: request.TimeEstimate
        );

        if (request.StatusId.HasValue && request.StatusId.Value != task.StatusId)
        {
            var isValid = await db.Statuses
                .AnyAsync(s => s.Id == request.StatusId.Value, cancellationToken);

            if (!isValid) return Result.Failure(Error.Validation("Task.InvalidStatus", "The requested status does not exist or does not belong to this workspace."));

            task.Update(statusId: request.StatusId.Value);
        }

        var affectedRows = await db.SaveChangesAsync(cancellationToken);
        if (affectedRows > 0)
        {
            logger.LogInformation("Broadcasting entity updates for updated task {TaskId}", task.Id);
            _ =  realtimeService
            .NotifyEntitiesUpdatedAsync(
                workspaceContext.WorkspaceId,
                new EntityBatchUpdate { Tasks = new List<TaskRecord> { TaskRecord.FromDomain(task) } },
                default)
            .ContinueWith(t =>
                logger.LogError(t.Exception, "Failed to send real-time notification for updated task {TaskId}", task.Id), 
                TaskContinuationOptions.OnlyOnFaulted);
                
            logger.LogInformation("Successfully updated task {TaskId}", task.Id);
        }
        else
        {
            logger.LogError("Failed to save updates for task {TaskId}", task.Id);
        }

        return Result.Success();
    }

}
