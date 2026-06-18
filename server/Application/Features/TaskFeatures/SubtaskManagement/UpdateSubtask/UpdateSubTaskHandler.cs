using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application;

public class UpdateSubTaskHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    RealtimeService realtimeService,
    ILogger<UpdateSubTaskHandler> logger,
    PermissionService permissionService
) : ICommandHandler<UpdateSubTaskCommand>
{
    public async Task<Result> Handle(UpdateSubTaskCommand command, CancellationToken cancellationToken)
    {
        var parentTask = await db.ProjectTasks
            .AsNoTracking()
            .Where(t => t.Id == command.ParentTaskId && t.DeletedAt == null)
            .Select(t => new { t.Id, t.ProjectSpaceId, t.CreatorId })
            .FirstOrDefaultAsync(cancellationToken);
        if (parentTask is null) return Result.Failure(TaskError.NotFound);

        var hasAccess = await permissionService.VerifyAsync(Role.Member, parentTask.ProjectSpaceId, AccessLevel.Editor, parentTask.CreatorId, cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var subTask = await db.ProjectTasks
            .FirstOrDefaultAsync(t =>
                t.Id == command.TaskId &&
                t.ParentTaskId == command.ParentTaskId &&
                t.DeletedAt == null,
                cancellationToken);

        if (subTask is null) return Result.Failure(TaskError.NotFound);

        if (subTask.ProjectSpaceId != parentTask.ProjectSpaceId) return Result.Failure(MemberError.DontHavePermission);

        var newSlug = command.Name is not null && command.Name != subTask.Name
            ? SlugHelper.GenerateSlug(command.Name)
            : null;

        subTask.Update(
            name: command.Name,
            slug: newSlug,
            priority: command.Priority
        );

        var affectedRows = await db.SaveChangesAsync(cancellationToken);
        if (affectedRows > 0)
        {
            _ = realtimeService
            .NotifyEntitiesUpdatedAsync(
                workspaceContext.WorkspaceId,
                new EntityBatchUpdate { Tasks = [TaskRecord.FromDomain(subTask)] },
                default)
            .ContinueWith(t =>
                logger.LogError(t.Exception, "Failed to send real-time notification for updated subtask {SubTaskId}", subTask.Id),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        return Result.Success();
    }
}