using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.FolderFeatures;

public class UpdateFolderHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<UpdateFolderCommand>
{
    public async Task<Result> Handle(UpdateFolderCommand request, CancellationToken ct)
    {
        var folder = await db.Folders.ById(request.FolderId).FirstOrDefaultAsync(ct);
        if (folder == null) 
            return Result.Failure(FolderError.NotFound);

        if (context.CurrentMember.Role > Role.Admin && folder.CreatorId != context.CurrentMember.Id)
            return Result.Failure(MemberError.DontHavePermission);

        if (request.Name is not null)
        {
            folder.UpdateName(request.Name);
            folder.UpdateSlug(SlugHelper.GenerateSlug(request.Name));
        }
        
        if (request.Description is not null)
        {
            folder.UpdateDescription(request.Description);
        }

        if (request.Color is not null) folder.UpdateColor(request.Color);
        if (request.Icon is not null) folder.UpdateIcon(request.Icon);

        if (request.IsPrivate.HasValue) 
            folder.UpdatePrivate(request.IsPrivate.Value);

        if (request.StartDate != null) folder.UpdateStartDate(request.StartDate.Value);
        if (request.DueDate != null) folder.UpdateDueDate(request.DueDate.Value);

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
