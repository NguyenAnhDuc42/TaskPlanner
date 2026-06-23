using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace Application;

public class UpdateSpaceHandler(
    TaskPlanDbContext db, 
    WorkspaceContext context, 
    PermissionService permissionService, 
    RealtimeService realtimeService, 
    ILogger<UpdateSpaceHandler> logger) 
    : ICommandHandler<UpdateSpaceCommand>
{
    public async Task<Result> Handle(UpdateSpaceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to update space {SpaceId}", request.SpaceId);
        var space = await db.ProjectSpaces.FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.DeletedAt == null, cancellationToken);

        if (space == null) 
        {
            logger.LogWarning("Space {SpaceId} not found or deleted", request.SpaceId);
            return Result.Failure(SpaceError.NotFound);
        }
        if (space.ProjectWorkspaceId != context.WorkspaceId) return Result.Failure(SpaceError.NotFound);

        var hasAccess = await permissionService.VerifyAsync(Role.Member, request.SpaceId, AccessLevel.Manager, space.CreatorId, cancellationToken);
        if (!hasAccess) 
        {
            logger.LogWarning("Access denied for user {UserId} to update space {SpaceId}", context.CurrentMember.Id, space.Id);
            return Result.Failure(MemberError.DontHavePermission);
        }

        var slug = request.Name != null ? SlugHelper.GenerateSlug(request.Name) : null;

        space.Update(
            name: request.Name,
            slug: slug,
            color: request.Color,
            icon: request.Icon,
            isPrivate: request.IsPrivate
        );
        var affectedRows = await db.SaveChangesAsync(cancellationToken);
        if (affectedRows > 0)
        {
            logger.LogInformation("Broadcasting entity updates for updated space {SpaceId}", space.Id);
            _ = realtimeService
            .NotifyEntitiesUpdatedAsync(
                context.TryGetWorkspaceId().Value,
                new EntityBatchUpdate { Spaces = [SpaceRecord.FromDomain(space)] },
                default)
            .ContinueWith(t =>
                logger.LogError(t.Exception, "Failed to send real-time notification for updated space {SpaceId}", space.Id), 
                TaskContinuationOptions.OnlyOnFaulted);
        }

        return Result.Success();
    }
}



