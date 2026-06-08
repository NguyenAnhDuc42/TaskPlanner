using Microsoft.EntityFrameworkCore;

namespace Application;

public class UpdateSubTaskHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    RealtimeService realtimeService,
    PermissionService permissionService
) : ICommandHandler<UpdateSubTaskCommand>
{
    public async Task<Result> Handle(UpdateSubTaskCommand command, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Member, command.SpaceId, AccessLevel.Editor, cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var subTask = await db.ProjectTasks
            .FirstOrDefaultAsync(t =>
                t.Id == command.TaskId &&
                t.ParentTaskId == command.ParentTaskId &&
                t.DeletedAt == null,
                cancellationToken);

        if (subTask is null) return Result.Failure(TaskError.NotFound);

        if (subTask.ProjectSpaceId != command.SpaceId) return Result.Failure(MemberError.DontHavePermission);

        var newSlug = command.Name is not null && command.Name != subTask.Name
            ? SlugHelper.GenerateSlug(command.Name)
            : null;

        subTask.Update(
            name: command.Name,
            slug: newSlug,
            priority: command.Priority
        );

        await db.SaveChangesAsync(cancellationToken);

        await realtimeService.NotifyEntitiesUpdatedAsync(
            context.TryGetWorkspaceId().Value,
            new EntityBatchUpdate { Tasks = [TaskRecord.FromDomain(subTask)] },
            cancellationToken);

        return Result.Success();
    }
}