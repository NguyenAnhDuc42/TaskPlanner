using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.FolderFeatures.SelfManagement.DeleteFolder;

public class DeleteFolderHandler(
    IDataBase db, 
    WorkspaceContext context,
    IBackgroundJobService backgroundJob,
    IRealtimeService realtime
) : ICommandHandler<DeleteFolderCommand>
{
    public async Task<Result> Handle(DeleteFolderCommand request, CancellationToken ct)
    {
        var folder = await db.Folders.ById(request.FolderId).FirstOrDefaultAsync(ct);
        if (folder == null) 
            return Result.Failure(FolderError.NotFound);

        // AUTHORIZATION: Only Admin/Owner or the folder creator (MemberId) can delete a folder
        if (context.CurrentMember.Role > Role.Admin && folder.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);

        // 1. Logic: Use formal domain method to trigger background cleanup
        folder.Delete(context.workspaceId, context.CurrentMember.Id);

        await db.SaveChangesAsync(ct);
        
        // 2. Instant Trigger for background cleanup (Tasks, Views)
        backgroundJob.TriggerOutbox();

        // 3. STAGE 1 Notification: UI hides the folder immediately
        await realtime.NotifyWorkspaceAsync(context.workspaceId, "FolderDeleting", new { FolderId = request.FolderId, SpaceId = folder.ProjectSpaceId, WorkspaceId = context.workspaceId }, ct);

        return Result.Success();
    }
}
