using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces;

namespace Application.Features.FolderFeatures;

public class CreateFolderHandler(
    IDataBase db, 
    WorkspaceContext context,
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
        

        var maxKey = await db.Folders
            .AsNoTracking()
            .BySpace(request.spaceId)
            .WhereNotDeleted()
            .MaxAsync(f => f.OrderKey, ct);
        
        var orderKey = maxKey is null ? FractionalIndex.Start() : FractionalIndex.After(maxKey);
        var slug = SlugHelper.GenerateSlug(request.name);

        var folder = ProjectFolder.Create(
            projectWorkspaceId: context.workspaceId,
            projectSpaceId: space.Id,
            name: request.name,
            slug: slug,
            description: request.description ?? string.Empty,
            orderKey: orderKey,
            isPrivate: request.isPrivate,
            creatorId: context.CurrentMember.Id,
            color: request.color,
            icon: request.icon,
            startDate: request.startDate,
            dueDate: request.dueDate
        );

        await db.Folders.AddAsync(folder, ct);

        // Inline view creation
        db.ViewDefinitions.AddRange(
            ViewDefinition.CreateDefaults(context.workspaceId, space.Id, folder.Id, context.CurrentMember.Id));

        await db.SaveChangesAsync(ct);

        await realtime.NotifyWorkspaceAsync(context.workspaceId, "FolderCreated", new { FolderId = folder.Id, SpaceId = space.Id, WorkspaceId = context.workspaceId }, ct);

        return Result<Guid>.Success(folder.Id);
    }
}
