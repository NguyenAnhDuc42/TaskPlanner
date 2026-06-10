using Microsoft.EntityFrameworkCore;

namespace Application;

public class DeleteSubTaskHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    RealtimeService realtimeService,
    PermissionService permissionService
) : ICommandHandler<DeleteSubTaskCommand>
{
    public async Task<Result> Handle(DeleteSubTaskCommand command, CancellationToken cancellationToken)
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

        subTask.SoftDelete();
        await db.SaveChangesAsync(cancellationToken);

        await realtimeService.NotifyEntitiesDeletedAsync(
            workspaceContext.WorkspaceId,
            new EntityBatchDelete { TaskIds = [subTask.Id] },
            cancellationToken);

        return Result.Success();
    }
}
