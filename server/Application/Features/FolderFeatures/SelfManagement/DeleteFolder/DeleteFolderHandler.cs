using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.FolderFeatures.SelfManagement.DeleteFolder;

public class DeleteFolderHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<DeleteFolderCommand>
{
    public async Task<Result> Handle(DeleteFolderCommand request, CancellationToken ct)
    {
        var folder = await db.Folders.ById(request.FolderId).FirstOrDefaultAsync(ct);
        if (folder == null) return Result.Failure(FolderError.NotFound);

        // Security Resolve: Check if member has access to this space and correct role
        if (folder.ProjectSpace.ProjectWorkspaceId != context.workspaceId)
            return Result.Failure(MemberError.DontHavePermission);

        if (context.CurrentMember.Role > Role.Admin && folder.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);

        folder.SoftDelete();
        
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
