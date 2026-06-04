using Microsoft.EntityFrameworkCore;
namespace Application;

public class UpdateTaskHandler(TaskPlanDbContext db, WorkspaceContext context, RealtimeService realtime) : ICommandHandler<UpdateTaskCommand>
{
    public async Task<Result> Handle(UpdateTaskCommand request, CancellationToken ct)
    {
        var task = await db.ProjectTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, ct);

        if (task == null) return Result.Failure(TaskError.NotFound);

        var statusUpdateResult = await ApplyStatusAndUpdateProperties(task, request, ct);
        if (!statusUpdateResult.IsSuccess) return statusUpdateResult;

        await db.SaveChangesAsync(ct);

        await realtime.NotifyWorkspaceAsync(context.workspaceId, "TaskUpdated", new { 
            TaskId = task.Id, 
            WorkspaceId = context.workspaceId,
            Name = task.Name,
            StatusId = task.StatusId,
            Priority = task.Priority,
            StoryPoints = task.StoryPoints,
            TimeEstimate = task.TimeEstimateSeconds,
            StartDate = task.StartDate,
            DueDate = task.DueDate,
            Icon = task.Icon,
            Color = task.Color
        }, ct);

        return Result.Success();
    }

    private async Task<Result> ApplyStatusAndUpdateProperties(ProjectTask task, UpdateTaskCommand request, CancellationToken ct)
    {
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
                .AnyAsync(s => s.Id == request.StatusId.Value && s.ProjectWorkspaceId == task.ProjectWorkspaceId, ct);

            if (!isValid)
                return Result.Failure(Error.Validation("Task.InvalidStatus", "The requested status does not exist or does not belong to this workspace."));

            task.Update(statusId: request.StatusId.Value);
        }

        return Result.Success();
    }
}
