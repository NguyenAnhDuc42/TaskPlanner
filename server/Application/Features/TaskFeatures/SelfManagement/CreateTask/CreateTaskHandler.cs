using Microsoft.EntityFrameworkCore;
namespace Application;

public class CreateTaskHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, PermissionService permissionService, RealtimeService realtimeService) : ICommandHandler<CreateTaskCommand>
{
    public async Task<Result> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {

        var hasPermission = await VerifyPermission(request.ParentType, request.ParentId, permissionService, cancellationToken);
        if (!hasPermission) return Result.Failure(MemberError.DontHavePermission);
        var ancestors = await HierarchyHelper.GetAncestorChain(db, request.ParentId, request.ParentType, cancellationToken);
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


            return Result.Success();
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await realtimeService.NotifyEntitiesUpdatedAsync(
             workspaceContext.WorkspaceId,
             new EntityBatchUpdate
             {
                 Tasks = [TaskRecord.FromDomain(task!)],
                 Assignees = assigee.Select(AssigneeRecord.FromDomain).ToList()
             },
             cancellationToken);
        }

        return result;
    }


    private async Task<string> ResolveFolderOrderKey(Guid folderId, CancellationToken ct)
    {
        var maxKey = await db.ProjectTasks.AsNoTracking().Where(t => t.ProjectFolderId == folderId).MaxAsync(t => (string?)t.OrderKey, ct);
        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }

    private async Task<string> ResolveSpaceOrderKey(Guid spaceId, CancellationToken ct)
    {
        var maxKey = await db.ProjectTasks.AsNoTracking().Where(t => t.ProjectSpaceId == spaceId).MaxAsync(t => (string?)t.OrderKey, ct);
        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }


    private async Task<bool> VerifyPermission(EntityLayerType entityType, Guid entityId, PermissionService permissionService, CancellationToken cancellationToken)
    {
        switch (entityType)
        {
            case EntityLayerType.ProjectFolder:
                var isFolderCreator = await db.ProjectFolders
                    .AsNoTracking()
                    .Where(f => f.Id == entityId && f.DeletedAt == null)
                    .Select(f => new { f.CreatorId })
                    .FirstOrDefaultAsync(cancellationToken);
                if (isFolderCreator?.CreatorId != workspaceContext.CurrentMember.Id)
                {
                    var hasAccess = await permissionService.VerifyAsync(Role.Member, entityId, AccessLevel.Editor, cancellationToken);
                    if (!hasAccess) return false;
                }
                break;
            case EntityLayerType.ProjectSpace:
                var isSpaceCreator = await db.ProjectSpaces
                    .AsNoTracking()
                    .Where(s => s.Id == entityId && s.DeletedAt == null)
                    .Select(s => new { s.CreatorId })
                    .FirstOrDefaultAsync(cancellationToken);
                if (isSpaceCreator?.CreatorId != workspaceContext.CurrentMember.Id)
                {
                    var hasAccess = await permissionService.VerifyAsync(Role.Member, entityId, AccessLevel.Editor, cancellationToken);
                    if (!hasAccess) return false;
                }
                break;
            default:
                return false;
        }
        return false;
    }

    private async Task<List<TaskAssignment>> CreateAssignmentsAsync(CreateTaskCommand request, Guid workspaceId, Guid taskId, CancellationToken cancellationToken)
    {
        if (request.AssigneeIds?.Any() != true)
            return [];

        var memberIds = await db.WorkspaceMembers
            .Where(wm =>
                wm.ProjectWorkspaceId == workspaceId &&
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


