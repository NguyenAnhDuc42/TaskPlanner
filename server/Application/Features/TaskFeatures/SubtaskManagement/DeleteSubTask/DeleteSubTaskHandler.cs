using Microsoft.EntityFrameworkCore;

namespace Application;

public class DeleteSubTaskHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    RealtimeService realtimeService,
    PermissionService permissionService
) : ICommandHandler<DeleteSubTaskCommand>
{
    public async Task<Result> Handle(DeleteSubTaskCommand command, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Member, command.SpaceId, AccessLevel.Editor, cancellationToken);
        if (!hasAccess)
            return Result.Failure(MemberError.DontHavePermission);

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
            context.TryGetWorkspaceId().Value,
            new EntityBatchDelete { TaskIds = [subTask.Id] },
            cancellationToken);

        return Result.Success();
    }
}
