using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.FolderFeatures.SelfManagement.UpdateFolder;

public class UpdateFolderHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<UpdateFolderCommand>
{
    public async Task<Result> Handle(UpdateFolderCommand request, CancellationToken ct)
    {
        var folder = await db.Folders.ById(request.FolderId).FirstOrDefaultAsync(ct);
        if (folder == null) 
            return Result.Failure(FolderError.NotFound);

        // AUTHORIZATION: Reverting logic to use MemberId (context.CurrentMember.Id)
        if (context.CurrentMember.Role > Role.Admin && folder.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);

        if (request.Name is not null || request.Description is not null)
        {
            var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;
            folder.UpdateBasicInfo(request.Name, slug, request.Description);
        }

        if (request.Color is not null || request.Icon is not null) 
            folder.UpdateCustomization(request.Color, request.Icon);

        if (request.IsPrivate.HasValue) 
            folder.UpdatePrivate(request.IsPrivate.Value);

        if (request.StartDate != null || request.DueDate != null)
        {
            folder.UpdateDates(
                request.StartDate ?? folder.StartDate,
                request.DueDate ?? folder.DueDate);
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
