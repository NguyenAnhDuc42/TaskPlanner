using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
namespace Application;

public class UpdateWorkspaceHandler(
    TaskPlanDbContext db, 
    WorkspaceContext context,
    HybridCache cache, 
    RealtimeService realtime
) : ICommandHandler<UpdateWorkspaceCommand>
{
    public async Task<Result> Handle(UpdateWorkspaceCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var workspace = await db.ProjectWorkspaces
            .ById(request.Id)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        var slug = !string.IsNullOrEmpty(request.Name) ? SlugHelper.GenerateSlug(request.Name) : null;

        workspace.Update(
            name: request.Name,
            slug: slug,
            description: request.Description,
            color: request.Color,
            icon: request.Icon,
            strictJoin: request.StrictJoin
        );

        if (request.Theme.HasValue) context.CurrentMember.UpdateTheme(request.Theme.Value);

        await db.SaveChangesAsync(ct);

        // Cache Invalidation
        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(context.CurrentMember.UserId), ct);
        await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceMembersTag(workspace.Id), ct);

        _ = realtime.NotifyUserAsync(context.CurrentMember.UserId, "WorkspaceUpdated", new { WorkspaceId = workspace.Id }, ct);

        return Result.Success();
    }
}



