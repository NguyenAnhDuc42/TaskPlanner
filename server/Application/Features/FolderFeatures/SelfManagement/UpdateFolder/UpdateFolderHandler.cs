using Application.Helpers;
using Application.Common.Errors;
using Application.Common.Results;
using Domain.Entities.ProjectEntities;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;
using Application.Interfaces.Data;

namespace Application.Features.FolderFeatures.SelfManagement.UpdateFolder;

public class UpdateFolderHandler : ICommandHandler<UpdateFolderCommand>
{
    private readonly IDataBase _db;

    public UpdateFolderHandler(IDataBase db)
    {
        _db = db;
    }

    public async Task<Result> Handle(UpdateFolderCommand request, CancellationToken ct)
    {
        var folder = await _db.Folders.ById(request.FolderId).FirstOrDefaultAsync(ct);
        if (folder == null) return Result.Failure(FolderError.NotFound);

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

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
