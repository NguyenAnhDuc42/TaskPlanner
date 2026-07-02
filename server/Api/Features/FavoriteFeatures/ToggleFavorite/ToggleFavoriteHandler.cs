using Microsoft.EntityFrameworkCore;

namespace Api;

public class ToggleFavoriteHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext,
    SyncPermissionService syncPermission,
    IdempotencyService idempotencyService,
    ILogger<ToggleFavoriteHandler> logger
) : ICommandHandler<ToggleFavoriteCommand, ToggleFavoriteResult>
{
    public async Task<Result<ToggleFavoriteResult>> Handle(ToggleFavoriteCommand request, CancellationToken cancellationToken)
    {
        syncPermission.RequireMember();

        var memberId = workspaceContext.CurrentMember?.Id ?? Guid.Empty;
        var workspaceId = workspaceContext.WorkspaceId;

        return await db.ExecuteInTransactionAsync(async () =>
        {
            // A retried toggle must not double-flip — return the already-applied result instead
            // of mutating again (unlike other handlers, there's no SyncEventId the client can
            // ignore on bypass; ToggleFavoriteResult IS the state the client needs back).
            var hasProcessed = await idempotencyService.HasProcessedAsync(request.TraceId, cancellationToken);
            if (hasProcessed)
            {
                var current = await db.Favorites.AsNoTracking().FirstOrDefaultAsync(
                    f => f.WorkspaceMemberId == memberId && f.EntityId == request.EntityId && f.EntityLayerType == request.EntityLayerType,
                    cancellationToken);

                logger.LogInformation("Idempotent bypass for trace {TraceId}. Returning current state.", request.TraceId);
                return Result<ToggleFavoriteResult>.Success(new ToggleFavoriteResult(current != null, current?.OrderKey, request.EntityId, request.EntityLayerType));
            }

            var existing = await db.Favorites.FirstOrDefaultAsync(
                f => f.WorkspaceMemberId == memberId && f.EntityId == request.EntityId && f.EntityLayerType == request.EntityLayerType,
                cancellationToken);

            idempotencyService.MarkAsProcessed(request.TraceId);

            if (existing != null)
            {
                db.Favorites.Remove(existing);
                logger.LogInformation("Removed favorite {EntityId} ({EntityLayerType}) for member {MemberId}", request.EntityId, request.EntityLayerType, memberId);
                return Result<ToggleFavoriteResult>.Success(new ToggleFavoriteResult(false, null, request.EntityId, request.EntityLayerType));
            }

            var favorite = new Favorite(workspaceId)
            {
                WorkspaceMemberId = memberId,
                EntityId = request.EntityId,
                EntityLayerType = request.EntityLayerType,
                OrderKey = request.OrderKey
            };
            db.Favorites.Add(favorite);

            logger.LogInformation("Added favorite {EntityId} ({EntityLayerType}) for member {MemberId}", request.EntityId, request.EntityLayerType, memberId);
            return Result<ToggleFavoriteResult>.Success(new ToggleFavoriteResult(true, favorite.OrderKey, request.EntityId, request.EntityLayerType));
        }, cancellationToken);
    }
}
