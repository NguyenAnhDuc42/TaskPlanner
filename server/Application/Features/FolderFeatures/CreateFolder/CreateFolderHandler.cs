using Microsoft.EntityFrameworkCore;
namespace Application;

public class CreateFolderHandler(
    TaskPlanDbContext db, 
    WorkspaceContext context,
    RealtimeService realtime
) : ICommandHandler<CreateFolderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateFolderCommand request, CancellationToken ct)
    {
        var space = await db.ProjectSpaces.FirstOrDefaultAsync(s => s.Id == request.spaceId, ct);
        if (space == null) 
            return Result<Guid>.Failure(SpaceError.NotFound);

        if (space.ProjectWorkspaceId != context.workspaceId)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        return await db.ExecuteInTransactionAsync(async () =>
        {
            var maxKey = await db.ProjectFolders
                .AsNoTracking()
                .Where(f => f.ProjectSpaceId == request.spaceId && f.DeletedAt == null)
                .MaxAsync(f => f.OrderKey, ct);
            
            var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
            var slug = SlugHelper.GenerateSlug(request.name);

            // 2. Create the folder 
            var folder = ProjectFolder.Create(
                projectWorkspaceId: context.workspaceId,
                projectSpaceId: space.Id,
                name: request.name,
                slug: slug,
                orderKey: orderKey,
                creatorId: context.CurrentMember.Id,
                color: request.color,
                icon: request.icon,
                startDate: request.startDate,
                dueDate: request.dueDate
            );

            if (request.statusId != null)
            {
                folder.Update(statusId: request.statusId);
            }

            await db.ProjectFolders.AddAsync(folder, ct);

            // 3. Create Default Workflow for the folder
            var workflow = Workflow.Create(
                context.workspaceId, 
                $"{request.name} Workflow", 
                $"Default workflow for {request.name} folder", 
                context.CurrentMember.Id, 
                projectFolderId: folder.Id
            );
            await db.Workflows.AddAsync(workflow, ct);

            // 4. Create Starter Statuses
            var statuses = Status.CreateFolderStarterSet(context.workspaceId, workflow.Id, context.CurrentMember.Id);
            await db.Statuses.AddRangeAsync(statuses, ct);

            // 5. Create Default Views
            db.ViewDefinitions.AddRange(
                ViewDefinition.CreateDefaults(context.workspaceId, space.Id, folder.Id, context.CurrentMember.Id));

            await realtime.NotifyWorkspaceAsync(context.workspaceId, "FolderCreated", new { FolderId = folder.Id, SpaceId = space.Id, WorkspaceId = context.workspaceId }, ct);

            return Result<Guid>.Success(folder.Id);
        }, ct);
    }
}



