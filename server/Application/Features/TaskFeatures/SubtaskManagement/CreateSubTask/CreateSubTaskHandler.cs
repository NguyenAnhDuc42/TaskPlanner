using Microsoft.EntityFrameworkCore;

namespace Application;

public class CreateSubTaskHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    RealtimeService realtimeService,
    PermissionService permissionService
) : ICommandHandler<CreateSubTaskCommand>
{
    public async Task<Result> Handle(CreateSubTaskCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Member, request.SpaceId, AccessLevel.Editor, cancellationToken);
        if (!hasAccess)
            return Result.Failure(MemberError.DontHavePermission);

        // Reads outside the transaction — minimal projection
        var parentTask = await db.ProjectTasks
            .Where(t => t.Id == request.ParentTaskId && t.DeletedAt == null)
            .Select(t => new { t.Id, t.ProjectWorkspaceId, t.ProjectSpaceId, t.ProjectFolderId })
            .FirstOrDefaultAsync(cancellationToken);

        if (parentTask is null)
            return Result.Failure(Error.NotFound("Task.NotFound", "Parent task not found"));

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
                parentTask.ProjectWorkspaceId,
                request.Name,
                context.CurrentMember.Id
            );
            db.Documents.Add(document);

            subTask = ProjectTask.Create(
               projectWorkspaceId: parentTask.ProjectWorkspaceId,
               projectSpaceId: parentTask.ProjectSpaceId,
               projectFolderId: parentTask.ProjectFolderId,
               name: request.Name,
               slug: SlugHelper.GenerateSlug(request.Name),
               defaultDocumentId: document.Id,
               color: "#FFFFFF",
               icon: null,
               creatorId: context.CurrentMember.Id,
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
                context.TryGetWorkspaceId().Value,
                new EntityBatchUpdate { Tasks = [TaskRecord.FromDomain(subTask)] },
                cancellationToken
            );
        }

        return result;
    }
}