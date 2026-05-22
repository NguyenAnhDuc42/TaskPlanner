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

        var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;

        folder.Update(
            name: request.Name,
            slug: slug,
            color: request.Color,
            icon: request.Icon,
            startDate: request.StartDate,
            dueDate: request.DueDate,
            priority: request.Priority
        );

        if (request.StatusId.HasValue && request.StatusId.Value != folder.StatusId)
        {
            var isValid = await db.Statuses
                .AnyAsync(s => s.Id == request.StatusId.Value && s.Workflow.ProjectWorkspaceId == folder.ProjectWorkspaceId, ct);

            if (!isValid)
                return Result.Failure(Error.Validation("Folder.InvalidStatus", "The requested status does not exist or does not belong to this workspace."));

            folder.Update(statusId: request.StatusId.Value);
        }

        await db.SaveChangesAsync(ct);

        await realtime.NotifyWorkspaceAsync(context.workspaceId, "FolderUpdated", new { 
            FolderId = folder.Id, 
            SpaceId = folder.ProjectSpaceId, 
            WorkspaceId = context.workspaceId,
            Name = folder.Name,
            Icon = folder.Icon,
            Color = folder.Color,
            StatusId = folder.StatusId,
            Priority = folder.Priority,
            StartDate = folder.StartDate,
            DueDate = folder.DueDate
        }, ct);

        return Result.Success();
    }
}
