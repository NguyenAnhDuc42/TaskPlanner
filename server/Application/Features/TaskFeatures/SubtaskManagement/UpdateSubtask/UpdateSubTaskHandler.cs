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

        var tasks = await db.ProjectTasks
            .Where(t => (t.Id == command.TaskId || t.Id == command.ParentTaskId) && t.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var parentTask = tasks.FirstOrDefault(t => t.Id == command.ParentTaskId && t.ParentTaskId == null);
        var subTask = tasks.FirstOrDefault(t => t.Id == command.TaskId && t.ParentTaskId == command.ParentTaskId);

        if (parentTask == null || subTask == null) 
            return Result.Failure(TaskError.NotFound);

        string? newSlug = null;
        if (command.Name != null && command.Name != subTask.Name)
        {
            newSlug = SlugHelper.GenerateSlug(command.Name);
        }

        subTask.Update(
            name: command.Name,
            slug: newSlug,
            priority: command.Priority
        );

        await db.SaveChangesAsync(cancellationToken);

        var record = new TaskRecord
        {
            Id = subTask.Id,
            WorkspaceId = subTask.ProjectWorkspaceId,
            Name = subTask.Name,
            CreatedAt = subTask.CreatedAt,
            OrderKey = subTask.OrderKey,
            SpaceId = subTask.ProjectSpaceId,
            FolderId = subTask.ProjectFolderId,
            DefaultDocumentId = subTask.DefaultDocumentId,
            ParentType = subTask.ProjectFolderId != null ? "ProjectFolder" : "ProjectSpace",
            ParentTaskId = subTask.ParentTaskId,
            Priority = subTask.Priority
        };

        var updatePayload = new EntityBatchUpdate
        {
            Tasks = new List<TaskRecord> { record }
        };
        await realtimeService.NotifyEntitiesUpdatedAsync(context.workspaceId, updatePayload, cancellationToken);

        return Result.Success();
    }
}