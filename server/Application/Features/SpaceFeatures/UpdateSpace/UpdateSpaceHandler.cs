using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces;
using Application.Interfaces.Data;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SpaceFeatures;

public class UpdateSpaceHandler(IDataBase db, WorkspaceContext context, IRealtimeService realtime) : ICommandHandler<UpdateSpaceCommand>
{
    public async Task<Result> Handle(UpdateSpaceCommand request, CancellationToken ct)
    {
        var space = await db.Spaces
            .ById(request.SpaceId)
            .FirstOrDefaultAsync(ct);

        if (space == null) 
            return Result.Failure(SpaceError.NotFound);

        if (space.ProjectWorkspaceId != context.workspaceId)
            return Result.Failure(MemberError.DontHavePermission);

        var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;

        space.Update(
            name: request.Name,
            slug: slug,
            color: request.Color,
            icon: request.Icon,
            isPrivate: request.IsPrivate
        );
        await db.SaveChangesAsync(ct);

        await realtime.NotifyWorkspaceAsync(context.workspaceId, "SpaceUpdated", new { 
            SpaceId = space.Id, 
            WorkspaceId = context.workspaceId,
            Name = space.Name,
            Icon = space.Icon,
            Color = space.Color,
            IsPrivate = space.IsPrivate
        }, ct);

        return Result.Success();
    }
}
