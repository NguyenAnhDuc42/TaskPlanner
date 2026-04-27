using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.FolderFeatures;

public class DeleteFolderHandler(
    IDataBase db, 
    WorkspaceContext context,
    IRealtimeService realtime
) : ICommandHandler<DeleteFolderCommand>
{
    public async Task<Result> Handle(DeleteFolderCommand request, CancellationToken ct)
    {
        var folder = await db.Folders.ById(request.FolderId).FirstOrDefaultAsync(ct);
        if (folder == null) 
            return Result.Failure(FolderError.NotFound);

        if (context.CurrentMember.Role > Role.Admin && folder.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);
            
        folder.Delete();

        await db.SaveChangesAsync(ct);
 
        await realtime.NotifyWorkspaceAsync(context.workspaceId, "FolderDeleting", new { FolderId = request.FolderId, SpaceId = folder.ProjectSpaceId, WorkspaceId = context.workspaceId }, ct);

        return Result.Success();
    }
}
