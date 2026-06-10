using Microsoft.EntityFrameworkCore;
namespace Application;

public class UpdateTaskHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, PermissionService permissionService, RealtimeService realtimeService) : ICommandHandler<UpdateTaskCommand>
{
    public async Task<Result> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId && t.DeletedAt == null, cancellationToken);
        if (task == null) return Result.Failure(TaskError.NotFound);

        var hasAccess = await permissionService.VerifyAsync(Role.Member, task.ProjectSpaceId, AccessLevel.Editor, task.CreatorId, cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;

        task.Update(
            name: request.Name,
            slug: slug,
            color: request.Color,
            icon: request.Icon,
            priority: request.Priority,
            startDate: request.StartDate,
            dueDate: request.DueDate,
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
            await realtimeService.NotifyEntitiesUpdatedAsync(
                workspaceContext.WorkspaceId,
                new EntityBatchUpdate { Tasks = new List<TaskRecord> { TaskRecord.FromDomain(task) } },
                cancellationToken);
        }

        return Result.Success();
    }

}
