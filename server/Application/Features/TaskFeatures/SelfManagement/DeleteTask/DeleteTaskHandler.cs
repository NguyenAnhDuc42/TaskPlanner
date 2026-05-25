using Microsoft.EntityFrameworkCore;

namespace Application;

public class DeleteTaskHandler(TaskPlanDbContext db, WorkspaceContext context, RealtimeService realtimeService) : ICommandHandler<DeleteTaskCommand>
{
    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken ct)
    {
        var task = await db.ProjectTasks
            .ById(request.TaskId)
            .FirstOrDefaultAsync(ct);

        if (task == null) 
            return Result.Failure(TaskError.NotFound);

        task.SoftDelete();
        await db.SaveChangesAsync(ct);

        await realtimeService.NotifyWorkspaceAsync(context.workspaceId, "TaskUpdated", new { TaskId = task.Id, FolderId = task.ProjectFolderId, SpaceId = task.ProjectSpaceId }, ct);

        return Result.Success();
    }
}


