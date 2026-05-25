using Microsoft.EntityFrameworkCore;
namespace Application;

public class CreateTaskHandler(TaskPlanDbContext db, WorkspaceContext context, RealtimeService realtimeService) : ICommandHandler<CreateTaskCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTaskCommand request, CancellationToken ct)
    {
        var ancestors = await HierarchyHelper.GetAncestorChain(db, request.ParentId, request.ParentType, ct);

        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            string orderKey = request.ParentType switch
            {
                EntityLayerType.ProjectFolder => await ResolveFolderOrderKey(request.ParentId, ct),
                EntityLayerType.ProjectSpace => await ResolveSpaceOrderKey(request.ParentId, ct),
                _ => FractionalIndex.Start()
            };

            if (request.StatusId.HasValue)
            {
                var isValidStatus = await db.Statuses.AnyAsync(s => 
                    s.Id == request.StatusId.Value && 
                    s.ProjectWorkspaceId == ancestors.ProjectWorkspaceId, ct);

                if (!isValidStatus)
                    return Result<Guid>.Failure(Error.Validation("Task.InvalidStatus", "The requested status does not exist or does not belong to this workspace."));
            }

            var slug = SlugHelper.GenerateSlug(request.Name);

            // 1. Create the primary document for this task
            var document = Document.Create(
                ancestors.ProjectWorkspaceId,
                request.Name, // Doc name matches task name initially
                context.CurrentMember.Id
            );
            await db.Documents.AddAsync(document, ct);

            // 2. Create the task linked to the document
            var task = ProjectTask.Create(
                projectWorkspaceId: ancestors.ProjectWorkspaceId,
                projectSpaceId: ancestors.ProjectSpaceId,
                projectFolderId: ancestors.ProjectFolderId,
                name: request.Name,
                slug: slug,
                defaultDocumentId: document.Id,
                color: request.Color ?? "#FFFFFF",
                icon: request.Icon,
                creatorId: context.CurrentMember.Id,
                statusId: request.StatusId,
                priority: request.Priority,
                startDate: request.StartDate,
                dueDate: request.DueDate,
                storyPoints: request.StoryPoints,
                timeEstimateSeconds: request.TimeEstimate,
                orderKey: orderKey
            );

            await db.ProjectTasks.AddAsync(task, ct);

            // Assignments
            if (request.AssigneeIds?.Any() == true)
            {
                var memberIds = await db.WorkspaceMembers
                    .Where(wm => wm.ProjectWorkspaceId == ancestors.ProjectWorkspaceId && request.AssigneeIds.Contains(wm.UserId))
                    .Select(wm => wm.Id)
                    .ToListAsync(ct);

                var assignments = memberIds.Select(m => TaskAssignment.Create(task.Id, m, context.CurrentMember.Id)).ToList();
                task.AddAsignees(assignments);
            }

            return Result<Guid>.Success(task.Id);
        }, ct);

        if (result.IsSuccess)
        {
            await realtimeService.NotifyWorkspaceAsync(context.workspaceId, "TaskUpdated", new { TaskId = result.Value, FolderId = request.ParentId }, ct);
        }

        return result;
    }

    private async Task<string> ResolveFolderOrderKey(Guid folderId, CancellationToken ct)
    {
        var maxKey = await db.ProjectTasks.ByFolder(folderId).WhereNotDeleted().MaxAsync(t => (string?)t.OrderKey, ct);
        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }

    private async Task<string> ResolveSpaceOrderKey(Guid spaceId, CancellationToken ct)
    {
        var maxKey = await db.ProjectTasks.BySpace(spaceId).Where(t => t.ProjectFolderId == null).WhereNotDeleted().MaxAsync(t => (string?)t.OrderKey, ct);
        return maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
    }

}


