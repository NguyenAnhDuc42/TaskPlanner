using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
namespace Application;

public class UpdateWorkspaceHandler(
    TaskPlanDbContext db,
    WorkspaceContext context,
    HybridCache cache,
    RealtimeService realtime,
    ILogger<UpdateWorkspaceHandler> logger,
    PermissionService permissionService
) : ICommandHandler<UpdateWorkspaceCommand>
{
    public async Task<Result> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var hasAccess = await permissionService.VerifyAsync(Role.Admin, cancellationToken: cancellationToken);
        if (!hasAccess) return Result.Failure(MemberError.DontHavePermission);

        var workspace = await db.ProjectWorkspaces
            .ById(request.Id)
            .FirstOrDefaultAsync(cancellationToken);

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

        var affected = await db.SaveChangesAsync(cancellationToken);

        if (affected > 0)
        {
            // Cache Invalidation
            await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceListTag(context.CurrentMember.UserId), cancellationToken);
            await cache.RemoveByTagAsync(WorkspaceCacheKeys.WorkspaceMembersTag(workspace.Id), cancellationToken);

            _ = realtime.NotifyEntitiesUpdatedAsync(workspace.Id, new EntityBatchUpdate
            {
                Workspaces = [WorkspaceRecord.FromDomain(workspace, context.CurrentMember)]
            }, cancellationToken)
            .ContinueWith(t =>
                logger.LogError(t.Exception, "Failed to send workspace update notification for workspace {WorkspaceId}", workspace.Id), 
                TaskContinuationOptions.OnlyOnFaulted
            );
        }

        return Result.Success();
    }
}



