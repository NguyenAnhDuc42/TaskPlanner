using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.FolderFeatures.SelfManagement.CreateFolder;

public class CreateFolderHandler(
    IDataBase db, 
    WorkspaceContext context,
    IBackgroundJobService backgroundJob,
    IRealtimeService realtime
) : ICommandHandler<CreateFolderCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateFolderCommand request, CancellationToken ct)
    {
        var space = await db.Spaces.ById(request.spaceId).FirstOrDefaultAsync(ct);
        if (space == null) 
            return Result<Guid>.Failure(SpaceError.NotFound);

        if (space.ProjectWorkspaceId != context.workspaceId)
            return Result<Guid>.Failure(MemberError.DontHavePermission);

        if (context.CurrentMember.Role > Role.Admin)
            return Result<Guid>.Failure(MemberError.DontHavePermission);
        
        var customization = Customization.Create(request.color, request.icon);

        var maxKey = await db.Folders
            .AsNoTracking()
            .BySpace(request.spaceId)
            .WhereNotDeleted()
            .MaxAsync(f => (string?)f.OrderKey, ct);
        
        var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
        var slug = SlugHelper.GenerateSlug(request.name);

        var folder = ProjectFolder.Create(
            projectWorkspaceId: context.workspaceId,
            projectSpaceId: space.Id,
            name: request.name,
            slug: slug,
            description: request.description,
            orderKey: orderKey,
            isPrivate: request.isPrivate,
            creatorId: context.CurrentMember.Id,
            customization: customization,
            startDate: request.startDate,
            dueDate: request.dueDate
        );

        await db.Folders.AddAsync(folder, ct);
        await db.SaveChangesAsync(ct);
        
        // 1. Instant Trigger for background seeding (Overview/Tasks views)
        backgroundJob.TriggerOutbox();

        // 2. STAGE 1 Notification: UI shows folder immediately
        await realtime.NotifyWorkspaceAsync(context.workspaceId, "FolderCreating", new { FolderId = folder.Id, SpaceId = space.Id, WorkspaceId = context.workspaceId }, ct);

        return Result<Guid>.Success(folder.Id);
    }
}
