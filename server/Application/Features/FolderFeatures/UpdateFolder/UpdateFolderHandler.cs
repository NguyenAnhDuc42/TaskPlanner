using Microsoft.EntityFrameworkCore;

namespace Application;

public class UpdateFolderHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, PermissionService permissionService, RealtimeService realtimeService) : ICommandHandler<UpdateFolderCommand>
{
    public async Task<Result> Handle(UpdateFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = await db.ProjectFolders.FirstOrDefaultAsync(f => f.Id == request.FolderId, cancellationToken);
        if (folder == null) return Result.Failure(FolderError.NotFound);

        var isCreator = folder.CreatorId == workspaceContext.CurrentMember.Id;
        if (!isCreator)
        {
            var hasAccess = await permissionService.VerifyAsync(Role.Member, folder.ProjectSpaceId, AccessLevel.Editor, cancellationToken);
            if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);
        }

        var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;

        folder.Update(
            name: request.Name,
            slug: request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null,
            color: request.Color,
            icon: request.Icon,
            startDate: request.StartDate,
            dueDate: request.DueDate,
            statusId: request.StatusId,
            priority: request.Priority
        );

        var affectedRows = await db.SaveChangesAsync(cancellationToken);
        if (affectedRows > 0)
        {
            await realtimeService.NotifyEntitiesUpdatedAsync(
                workspaceContext.TryGetWorkspaceId().Value,
                new EntityBatchUpdate { Folders = [FolderRecord.FromDomain(folder)] },
                cancellationToken);
        }

        return Result.Success();
    }
}



