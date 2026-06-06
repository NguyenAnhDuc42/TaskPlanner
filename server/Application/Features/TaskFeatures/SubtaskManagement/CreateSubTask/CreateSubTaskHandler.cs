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

        var parentTask = await db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == request.ParentTaskId, cancellationToken);
        if (parentTask is null)
            return Result.Failure(Error.NotFound("Task.NotFound", "Parent task not found"));

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var slug = SlugHelper.GenerateSlug(request.Name);

            var document = Document.Create(
                parentTask.ProjectWorkspaceId,
                request.Name,
                context.CurrentMember.Id
            );
            await db.Documents.AddAsync(document, cancellationToken);

            var lastSubTask = await db.ProjectTasks
                .Where(t => t.ParentTaskId == parentTask.Id && t.DeletedAt == null)
                .OrderByDescending(t => t.OrderKey)
                .FirstOrDefaultAsync(cancellationToken);
            var orderKey = lastSubTask is null ? FractionalIndex.Start() : FractionalIndex.After(lastSubTask.OrderKey);

            var subTask = ProjectTask.Create(
                projectWorkspaceId: parentTask.ProjectWorkspaceId,
                projectSpaceId: parentTask.ProjectSpaceId,
                projectFolderId: parentTask.ProjectFolderId,
                name: request.Name,
                slug: slug,
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

            await db.ProjectTasks.AddAsync(subTask, cancellationToken);

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
                ParentTaskId = subTask.ParentTaskId
            };

            return Result<TaskRecord>.Success(record);
        }, cancellationToken);

        if (result.IsSuccess)
        {
            var updatePayload = new EntityBatchUpdate
            {
                Tasks = new List<TaskRecord> { result.Value }
            };
            await realtimeService.NotifyEntitiesUpdatedAsync(context.workspaceId, updatePayload, cancellationToken);
        }

        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }
}