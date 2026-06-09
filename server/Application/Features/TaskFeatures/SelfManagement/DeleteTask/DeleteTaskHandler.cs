using Microsoft.EntityFrameworkCore;

namespace Application;

public class DeleteTaskHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, PermissionService permissionService, RealtimeService realtimeService) : ICommandHandler<DeleteTaskCommand>
{
    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId && t.DeletedAt == null, cancellationToken);
        if (task == null) return Result.Failure(TaskError.NotFound);

        var isCreator = task.CreatorId == workspaceContext.CurrentMember.Id;
        if (!isCreator)
        {
            var hasAccess = await permissionService.VerifyAsync(Role.Member, task.ProjectSpaceId, AccessLevel.Editor, cancellationToken);
            if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);
        }

        task.SoftDelete();
        var affectedRows = await db.SaveChangesAsync(cancellationToken);
        if (affectedRows > 0)
        {
            await realtimeService.NotifyEntitiesDeletedAsync(
                workspaceContext.WorkspaceId,
                new EntityBatchDelete { TaskIds = new List<Guid> { task.Id } },
                cancellationToken);
        }
        
        return Result.Success();
    }
}


