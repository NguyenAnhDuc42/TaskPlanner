using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Enums;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.FolderFeatures;

public class UpdateFolderHandler(IDataBase db, WorkspaceContext context, IRealtimeService realtime) : ICommandHandler<UpdateFolderCommand>
{
    public async Task<Result> Handle(UpdateFolderCommand request, CancellationToken ct)
    {
        var folder = await db.Folders.ById(request.FolderId).FirstOrDefaultAsync(ct);
        if (folder == null) 
            return Result.Failure(FolderError.NotFound);

        if (request.Name is not null)
        {
            folder.UpdateName(request.Name);
            folder.UpdateSlug(SlugHelper.GenerateSlug(request.Name));
        }

        if (request.Color is not null) folder.UpdateColor(request.Color);
        if (request.Icon is not null) folder.UpdateIcon(request.Icon);

        if (request.IsPrivate.HasValue) 
            folder.UpdatePrivate(request.IsPrivate.Value);

        if (request.StartDate != null) folder.UpdateStartDate(request.StartDate.Value);
        if (request.DueDate != null) folder.UpdateDueDate(request.DueDate.Value);
        
        if (request.StatusId.HasValue)
            folder.UpdateStatus(request.StatusId.Value);

        if (request.IsInheritingWorkflow.HasValue)
            folder.UpdateInheritWorkflow(request.IsInheritingWorkflow.Value);

        await db.SaveChangesAsync(ct);

        await realtime.NotifyWorkspaceAsync(context.workspaceId, "FolderUpdated", new { 
            FolderId = folder.Id, 
            SpaceId = folder.ProjectSpaceId, 
            WorkspaceId = context.workspaceId,
            Name = folder.Name,
            Icon = folder.Icon,
            Color = folder.Color,
            StatusId = folder.StatusId,
            IsPrivate = folder.IsPrivate,
            StartDate = folder.StartDate,
            DueDate = folder.DueDate
        }, ct);

        return Result.Success();
    }
}
