using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.UpdateWorkspace;

public class UpdateWorkspaceHandler(
    IDataBase db, 
    WorkspaceContext context,
    HybridCache cache, 
    IRealtimeService realtime
) : ICommandHandler<UpdateWorkspaceCommand>
{
    public async Task<Result> Handle(UpdateWorkspaceCommand request, CancellationToken ct)
    {
        // Permission Check: Redundant null check removed; PermissionDecorator guarantees context.CurrentMember
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var workspace = await db.Workspaces
            .ById(request.Id)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        // Update logic
        var nameChanged = !string.IsNullOrEmpty(request.Name) && workspace.Name != request.Name;
        if (nameChanged)
        {
            workspace.UpdateBasicInfo(request.Name, SlugHelper.GenerateSlug(request.Name!), request.Description);
        }
        else if (request.Description != null)
        {
            workspace.UpdateBasicInfo(workspace.Name, workspace.Slug, request.Description);
        }

        if (request.Color != null || request.Icon != null)
        {
            workspace.UpdateCustomization(request.Color, request.Icon);
        }

        if (request.Theme.HasValue) workspace.UpdateTheme(request.Theme.Value);
        if (request.StrictJoin.HasValue) workspace.UpdateStrictJoin(request.StrictJoin.Value);

        await db.SaveChangesAsync(ct);

        // Cache Invalidation
        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(context.CurrentMember.UserId), ct);
        await cache.RemoveByTagAsync(CacheConstants.Tags.WorkspaceMembers(workspace.Id), ct);

        _ = realtime.NotifyUserAsync(context.CurrentMember.UserId, "WorkspaceUpdated", new { WorkspaceId = workspace.Id }, ct);

        return Result.Success();
    }
}
