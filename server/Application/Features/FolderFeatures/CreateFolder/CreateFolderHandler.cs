using Microsoft.EntityFrameworkCore;
namespace Application;

public class CreateFolderHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService
) : ICommandHandler<CreateFolderCommand>
{
    public async Task<Result> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        var space = await db.ProjectSpaces
             .AsNoTracking()
             .Where(s => s.Id == request.SpaceId && s.DeletedAt == null)
             .Select(s => new { s.Id, s.CreatorId })
             .FirstOrDefaultAsync(cancellationToken);
        if (space is null) return Result.Failure(SpaceError.NotFound);

        var hasAccess = await permissionService.VerifyAsync(Role.Member, request.SpaceId, AccessLevel.Editor, space.CreatorId, cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        ProjectFolder? folder = null;
        Guid? createdWorkflowId = null;
        var result = await db.ExecuteInTransactionAsync(async () =>
        {
            var maxKey = await db.ProjectFolders
                  .AsNoTracking()
                  .Where(f => f.ProjectSpaceId == request.SpaceId && f.DeletedAt == null)
                  .Select(f => f.OrderKey)
                  .OrderByDescending(k => k)
                  .FirstOrDefaultAsync(cancellationToken);

            var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
            var slug = SlugHelper.GenerateSlug(request.Name);

            // 2. Create the folder 
            folder = ProjectFolder.Create(
                projectWorkspaceId: workspaceContext.WorkspaceId,
                projectSpaceId: space.Id,
                name: request.Name,
                slug: slug,
                orderKey: orderKey,
                creatorId: workspaceContext.CurrentMember.Id,
                color: request.Color,
                icon: request.Icon,
                startDate: request.StartDate,
                dueDate: request.DueDate,
                priority: request.Priority,
                statusId: request.StatusId
            );

            db.ProjectFolders.Add(folder);

            var workflow = Workflow.Create(
                workspaceContext.WorkspaceId,
                $"{request.Name} Workflow",
                $"Default workflow for {request.Name} folder",
                workspaceContext.CurrentMember.Id,
                projectFolderId: folder.Id
            );
            db.Workflows.Add(workflow);

            var statuses = Status.CreateFolderStarterSet(workspaceContext.WorkspaceId, workflow.Id, workspaceContext.CurrentMember.Id);
            db.Statuses.AddRange(statuses);

            createdWorkflowId = workflow.Id;

            return Result.Success();
        }, cancellationToken);
        if (result.IsSuccess)
        {
            await realtimeService.NotifyEntitiesUpdatedAsync(
                workspaceContext.WorkspaceId,
                new EntityBatchUpdate { Folders = [FolderRecord.FromDomain(folder!, createdWorkflowId)] },
                cancellationToken);
        }

        return result;
    }
}



