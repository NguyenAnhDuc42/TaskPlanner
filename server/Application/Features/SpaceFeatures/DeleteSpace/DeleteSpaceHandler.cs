using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace Application;

public class DeleteSpaceHandler(TaskPlanDbContext db, WorkspaceContext context, RealtimeService realtimeService, PermissionService permissionService, ILogger<DeleteSpaceHandler> logger)
    : ICommandHandler<DeleteSpaceCommand>
{
    public async Task<Result> Handle(DeleteSpaceCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to delete space {SpaceId}", request.SpaceId);
        var space = await db.ProjectSpaces.FirstOrDefaultAsync(s => s.Id == request.SpaceId && s.DeletedAt == null, cancellationToken);

        if (space == null) 
        {
            logger.LogWarning("Space {SpaceId} not found or already deleted", request.SpaceId);
            return Result.Failure(SpaceError.NotFound);
        }
        if (space.ProjectWorkspaceId != context.WorkspaceId) return Result.Failure(SpaceError.NotFound);

        var hasAccess = await permissionService.VerifyAsync(Role.Member, request.SpaceId, AccessLevel.Manager, space.CreatorId, cancellationToken);
        if (!hasAccess) 
        {
            logger.LogWarning("Access denied for user {UserId} to delete space {SpaceId}", context.CurrentMember.Id, space.Id);
            return Result.Failure(MemberError.DontHavePermission);
        }
        space.Delete();


        var affectedRows = await db.SaveChangesAsync(cancellationToken);
        if (affectedRows > 0)
        {
            logger.LogInformation("Broadcasting entity deletion for space {SpaceId}", space.Id);
            await realtimeService.NotifyEntitiesDeletedAsync(
                context.TryGetWorkspaceId().Value,
                new EntityBatchDelete { SpaceIds = [space.Id] },
                cancellationToken);
        }

        return Result.Success();
    }
}



