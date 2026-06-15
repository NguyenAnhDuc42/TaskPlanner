using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Application;

public class CreateFolderHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService,
    ILogger<CreateFolderHandler> logger
) : ICommandHandler<CreateFolderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to create folder '{FolderName}' in space {SpaceId}", request.Name, request.SpaceId);
        
        var space = await db.ProjectSpaces
             .AsNoTracking()
             .FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.DeletedAt == null, cancellationToken);
        if (space is null) 
        {
            logger.LogWarning("Space {SpaceId} not found or deleted", request.SpaceId);
            return Result<Guid>.Failure(SpaceError.NotFound);
        }

        var hasAccess = await permissionService.VerifyAsync(Role.Member, request.SpaceId, AccessLevel.Editor, space.CreatorId, cancellationToken);
        if (!hasAccess) 
        {
            logger.LogWarning("Access denied for user {UserId} to create folder in space {SpaceId}", workspaceContext.CurrentMember.Id, request.SpaceId);
            return Result<Guid>.Failure(MemberError.DontHavePermission);
        }

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

            logger.LogInformation("Successfully created folder {FolderId} and workflow {WorkflowId} in database", folder.Id, workflow.Id);
            return Result<Guid>.Success(folder.Id);
        }, cancellationToken);
        
        if (result.IsSuccess)
        {
            var spaceWorkflowId = await db.Workflows.Where(w => w.ProjectSpaceId == space.Id).Select(w => w.Id).FirstOrDefaultAsync(cancellationToken);
            var spaceRecord = SpaceRecord.FromDomain(space, spaceWorkflowId) with { HasFolders = true };
            
            logger.LogInformation("Broadcasting entity updates for created folder {FolderId}", folder!.Id);
            await realtimeService.NotifyEntitiesUpdatedAsync(
                workspaceContext.WorkspaceId,
                new EntityBatchUpdate { Folders = [FolderRecord.FromDomain(folder!, createdWorkflowId)], Spaces = [spaceRecord] },
                cancellationToken);
                
            logger.LogInformation("Successfully completed CreateFolderHandler for folder {FolderId}", folder.Id);
        }
        else
        {
            logger.LogError("Failed to create folder '{FolderName}' due to transaction failure", request.Name);
        }

        return result;
    }
}



