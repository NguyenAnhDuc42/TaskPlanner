using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Application;

public class CreateTaskHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    ILogger<CreateTaskHandler> logger
) : ICommandHandler<CreateTaskCommand, TaskRecord>
{
    public async Task<Result<TaskRecord>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to create task '{TaskName}' under {ParentType} {ParentId}", request.Name, request.ParentType, request.ParentId);

        var hasPermission = await VerifyPermission(request.ParentType, request.ParentId, permissionService, cancellationToken);
        if (!hasPermission)
        {
            logger.LogWarning("Access denied for user {UserId} to create task in {ParentType} {ParentId}", workspaceContext.CurrentMember.Id, request.ParentType, request.ParentId);
            return Result<TaskRecord>.Failure(MemberError.DontHavePermission);
        }
        var ancestors = await HierarchyHelper.GetAncestorChain(db, request.ParentId, request.ParentType);
        ProjectTask? task = null;
        List<TaskAssignment> assigee = [];

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            string orderKey = request.ParentType switch
            {
                EntityLayerType.ProjectFolder => await ResolveFolderOrderKey(request.ParentId, cancellationToken),
                EntityLayerType.ProjectSpace => await ResolveSpaceOrderKey(request.ParentId, cancellationToken),
                _ => FractionalIndex.Start()
            };

            var slug = SlugHelper.GenerateSlug(request.Name);

            var document = Document.Create(
                ancestors.ProjectWorkspaceId,
                request.Name,
                workspaceContext.CurrentMember.Id
            );
            db.Documents.Add(document);

            task = ProjectTask.Create(
                projectWorkspaceId: ancestors.ProjectWorkspaceId,
                projectSpaceId: ancestors.ProjectSpaceId,
                projectFolderId: ancestors.ProjectFolderId,
                name: request.Name,
                slug: slug,
                defaultDocumentId: document.Id,
                color: request.Color ?? "#FFFFFF",
                icon: request.Icon,
                creatorId: workspaceContext.CurrentMember.Id,
                statusId: request.StatusId,
                priority: request.Priority,
                startDate: request.StartDate,
                dueDate: request.DueDate,
                storyPoints: request.StoryPoints,
                timeEstimateSeconds: request.TimeEstimate,
                orderKey: orderKey
            );

            db.ProjectTasks.Add(task);
            assigee = await CreateAssignmentsAsync(
                request,
                ancestors.ProjectWorkspaceId,
                task.Id,
                cancellationToken);


            logger.LogInformation("Successfully created task {TaskId} in database", task.Id);
            return Result<TaskRecord>.Success(TaskRecord.FromDomain(task!));
        }, cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation("Broadcasting entity updates for created task {TaskId}", task!.Id);
            var update = new EntityBatchUpdate
            {
                Tasks = result.Value is not null ? [result.Value] : null,
                Assignees = assigee.Select(AssigneeRecord.FromDomain).ToList()
            };

            if (request.ParentType == EntityLayerType.ProjectFolder)
            {
                var folder = await db.ProjectFolders.AsNoTracking().FirstOrDefaultAsync(f => f.Id == request.ParentId, cancellationToken);
                if (folder != null)
                {
                    var workflowId = await db.Workflows.Where(w => w.ProjectFolderId == folder.Id).Select(w => w.Id).FirstOrDefaultAsync(cancellationToken);
                    update.Folders = [FolderRecord.FromDomain(folder, workflowId) with { HasTasks = true }];
                }
            }
            else if (request.ParentType == EntityLayerType.ProjectSpace)
            {
                var space = await db.ProjectSpaces.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ParentId, cancellationToken);
                if (space != null)
                {
                    var workflowId = await db.Workflows.Where(w => w.ProjectSpaceId == space.Id).Select(w => w.Id).FirstOrDefaultAsync(cancellationToken);
                    update.Spaces = [SpaceRecord.FromDomain(space, workflowId) with { HasTasks = true }];
                }
            }

            _ = realtimeService
            .NotifyEntitiesUpdatedAsync(workspaceContext.WorkspaceId,update,default)
            .ContinueWith(t =>
                logger.LogError(t.Exception, "Failed to send real-time notification for created task {TaskId}", task.Id), 
                TaskContinuationOptions.OnlyOnFaulted);

            logger.LogInformation("Successfully completed CreateTaskHandler for task {TaskId}", task.Id);
        }
        else
        {
            logger.LogError("Failed to create task '{TaskName}' due to transaction failure", request.Name);
        }

        return result;
    }


    private async Task<string> ResolveFolderOrderKey(Guid folderId, CancellationToken cancellationToken)
    {
        var maxKey = await db.ProjectTasks.AsNoTracking().Where(t => t.ProjectFolderId == folderId).MaxAsync(t => (string?)t.OrderKey, cancellationToken);
        return FractionalIndex.SafeAfter(maxKey);
    }

    private async Task<string> ResolveSpaceOrderKey(Guid spaceId, CancellationToken cancellationToken)
    {
        var maxKey = await db.ProjectTasks.AsNoTracking().Where(t => t.ProjectSpaceId == spaceId).MaxAsync(t => (string?)t.OrderKey, cancellationToken);
        return FractionalIndex.SafeAfter(maxKey);
    }


    private async Task<bool> VerifyPermission(EntityLayerType entityType, Guid entityId, PermissionService permissionService, CancellationToken cancellationToken)
    {
        switch (entityType)
        {
            case EntityLayerType.ProjectFolder:
                var folder = await db.ProjectFolders
                    .AsNoTracking()
                    .Where(f => f.Id == entityId && f.DeletedAt == null)
                    .Select(f => new { f.CreatorId, f.ProjectSpaceId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (folder == null) return false;

                if (folder.CreatorId != workspaceContext.CurrentMember.Id)
                {
                    var hasAccess = await permissionService.VerifyAsync(Role.Member, spaceId: folder.ProjectSpaceId, requiredAccess: AccessLevel.Editor, cancellationToken: cancellationToken);
                    if (!hasAccess) return false;
                }
                return true;

            case EntityLayerType.ProjectSpace:
                var isSpaceCreator = await db.ProjectSpaces
                    .AsNoTracking()
                    .Where(s => s.Id == entityId && s.DeletedAt == null)
                    .Select(s => new { s.CreatorId })
                    .FirstOrDefaultAsync(cancellationToken);

                if (isSpaceCreator == null) return false;

                if (isSpaceCreator.CreatorId != workspaceContext.CurrentMember.Id)
                {
                    var hasAccess = await permissionService.VerifyAsync(Role.Member, spaceId: entityId, requiredAccess: AccessLevel.Editor, cancellationToken: cancellationToken);
                    if (!hasAccess) return false;
                }
                return true;

            default:
                return false;
        }
    }

    private async Task<List<TaskAssignment>> CreateAssignmentsAsync(CreateTaskCommand request, Guid workspaceId, Guid taskId, CancellationToken cancellationToken)
    {
        if (request.AssigneeIds?.Any() != true)
            return [];

        var memberIds = await db.WorkspaceMembers
            .Where(wm =>
                request.AssigneeIds.Contains(wm.Id) &&
                wm.DeletedAt == null)
            .Select(wm => wm.Id)
            .ToListAsync(cancellationToken);

        var assignments = memberIds
            .Select(memberId =>
                TaskAssignment.Create(
                    taskId,
                    memberId,
                    workspaceContext.CurrentMember.Id))
            .ToList();

        db.TaskAssignments.AddRange(assignments);


        return assignments;
    }

}


