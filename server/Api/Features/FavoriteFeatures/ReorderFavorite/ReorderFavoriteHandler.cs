using Microsoft.EntityFrameworkCore;

namespace Api;

public class ReorderFavoriteHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    ILogger<ReorderFavoriteHandler> logger
) : ICommandHandler<ReorderFavoriteCommand>
{
    public async Task<Result> Handle(ReorderFavoriteCommand request, CancellationToken cancellationToken)
    {
        syncPermission.RequireMember();

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;

        var favorite = await db.Favorites.FirstOrDefaultAsync(
            f => f.WorkspaceMemberId == memberId && f.EntityId == request.EntityId && f.EntityLayerType == request.EntityLayerType,
            cancellationToken);

        if (favorite is null)
            return Result.Failure(FavoriteError.NotFound);

        favorite.OrderKey = request.OrderKey;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Reordered favorite {EntityId} ({EntityLayerType}) for member {MemberId}", request.EntityId, request.EntityLayerType, memberId);
        return Result.Success();
    }
}
