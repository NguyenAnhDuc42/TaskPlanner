using Microsoft.EntityFrameworkCore;

namespace Application;

public class DeleteFolderHandler(
    TaskPlanDbContext db, 
    WorkspaceContext context,
    RealtimeService realtime
) : ICommandHandler<DeleteFolderCommand>
{
    public async Task<Result> Handle(DeleteFolderCommand request, CancellationToken ct)
    {
        var folder = await db.ProjectFolders.FirstOrDefaultAsync(f => f.Id == request.FolderId, ct);
        if (folder == null) 
            return Result.Failure(FolderError.NotFound);

        folder.Delete();

        await db.SaveChangesAsync(ct);
 
        await realtime.NotifyWorkspaceAsync(context.workspaceId, "FolderDeleting", new { FolderId = request.FolderId, SpaceId = folder.ProjectSpaceId, WorkspaceId = context.workspaceId }, ct);

        return Result.Success();
    }
}



