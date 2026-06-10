using Microsoft.EntityFrameworkCore;

namespace Application;

public class CreateSubTaskHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    RealtimeService realtimeService,
    PermissionService permissionService
) : ICommandHandler<CreateSubTaskCommand>
{
    public async Task<Result> Handle(CreateSubTaskCommand request, CancellationToken cancellationToken)
    {
        var parentTask = await db.ProjectTasks
            .AsNoTracking()
            .Where(t => t.Id == request.ParentTaskId && t.DeletedAt == null)
            .Select(t => new { t.Id, t.ProjectSpaceId, t.ProjectFolderId,t.CreatorId })
            .FirstOrDefaultAsync(cancellationToken);

        if (parentTask is null) return Result.Failure(Error.NotFound("Task.NotFound", "Parent task not found"));
        
        var hasAccess = await permissionService.VerifyAsync(Role.Member, parentTask.ProjectSpaceId, AccessLevel.Editor,parentTask.CreatorId, cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var lastOrderKey = await db.ProjectTasks
            .Where(t => t.ParentTaskId == parentTask.Id && t.DeletedAt == null)
            .OrderByDescending(t => t.OrderKey)
            .Select(t => t.OrderKey)
            .FirstOrDefaultAsync(cancellationToken);

        var orderKey = lastOrderKey is null ? FractionalIndex.Start() : FractionalIndex.After(lastOrderKey);

        ProjectTask? subTask = null;
        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var document = Document.Create(
                workspaceContext.WorkspaceId,
                request.Name,
                workspaceContext.CurrentMember.Id
            );
            db.Documents.Add(document);

            subTask = ProjectTask.Create(
               projectWorkspaceId: workspaceContext.WorkspaceId,
               projectSpaceId: parentTask.ProjectSpaceId,
               projectFolderId: parentTask.ProjectFolderId,
               name: request.Name,
               slug: SlugHelper.GenerateSlug(request.Name),
               defaultDocumentId: document.Id,
               color: "#FFFFFF",
               icon: null,
               creatorId: workspaceContext.CurrentMember.Id,
               statusId: null,
               priority: request.Priority,
               startDate: null,
               dueDate: null,
               storyPoints: null,
               timeEstimateSeconds: null,
               orderKey: orderKey,
               parentTaskId: parentTask.Id
           );
            db.ProjectTasks.Add(subTask);

            return Result.Success();
        }, cancellationToken);

        
        if (result.IsSuccess && subTask is not null)
        {
            await realtimeService.NotifyEntitiesUpdatedAsync(
                workspaceContext.WorkspaceId,
                new EntityBatchUpdate { Tasks = [TaskRecord.FromDomain(subTask)] },
                cancellationToken
            );
        }

        return result;
    }
}