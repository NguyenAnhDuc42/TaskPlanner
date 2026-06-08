using Microsoft.EntityFrameworkCore;

namespace Application;

public class DeleteFolderHandler(
    TaskPlanDbContext db, 
    WorkspaceContext workspaceContext,
    PermissionService permissionService,
    RealtimeService realtimeService
) : ICommandHandler<DeleteFolderCommand>
{
    public async Task<Result> Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
    {
        var folder = await db.ProjectFolders.FirstOrDefaultAsync(f => f.Id == request.FolderId, cancellationToken);
        if (folder == null) return Result.Failure(FolderError.NotFound);

        var isCreator = folder.CreatorId == workspaceContext.CurrentMember.Id;
        if (!isCreator)
        {
            var hasAccess = await permissionService.VerifyAsync(Role.Member, folder.ProjectSpaceId, AccessLevel.Editor, cancellationToken);
            if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);
        }

        folder.Delete();

        var affectedRows = await db.SaveChangesAsync(cancellationToken);
        if (affectedRows > 0)
        {
            await realtimeService.NotifyEntitiesDeletedAsync(
                workspaceContext.TryGetWorkspaceId().Value,
                new EntityBatchDelete { FolderIds = [folder.Id] },
                cancellationToken);
        }
 
        return Result.Success();
    }
}



