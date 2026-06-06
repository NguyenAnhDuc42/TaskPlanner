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

        var tasks = await db.ProjectTasks
            .Where(t => (t.Id == command.TaskId || t.Id == command.ParentTaskId) && t.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var parentTask = tasks.FirstOrDefault(t => t.Id == command.ParentTaskId && t.ParentTaskId == null);
        var subTask = tasks.FirstOrDefault(t => t.Id == command.TaskId && t.ParentTaskId == command.ParentTaskId);

        if (parentTask == null || subTask == null)
            return Result.Failure(TaskError.NotFound);

        subTask.SoftDelete();
        await db.SaveChangesAsync(cancellationToken);

        var deletePayload = new EntityBatchDelete
        {
            TaskIds = new List<Guid> { subTask.Id }
        };
        await realtimeService.NotifyEntitiesDeletedAsync(context.workspaceId, deletePayload, cancellationToken);

        return Result.Success();
    }
}
