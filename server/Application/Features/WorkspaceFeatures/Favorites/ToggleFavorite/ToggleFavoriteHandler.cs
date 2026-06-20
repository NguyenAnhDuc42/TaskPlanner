using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application;

public class ToggleFavoriteHandler(TaskPlanDbContext db, WorkspaceContext workspaceContext, ILogger<ToggleFavoriteHandler> logger) : ICommandHandler<ToggleFavoriteCommand>
{
    public async Task<Result> Handle(ToggleFavoriteCommand request, CancellationToken cancellationToken)
    {
        var memberId = workspaceContext.CurrentMember.Id;

        var existing = await db.Favorites.FirstOrDefaultAsync(f => 
            f.WorkspaceMemberId == memberId && 
            f.EntityId == request.EntityId && 
            f.EntityLayerType == request.EntityLayerType, 
            cancellationToken);

        if (existing != null)
        {
            db.Favorites.Remove(existing);
            logger.LogInformation("Removed favorite for EntityId {EntityId} by Member {MemberId}", request.EntityId, memberId);
        }
        else
        {
            var newFav = new Favorite
            {
                WorkspaceMemberId = memberId,
                EntityId = request.EntityId,
                EntityLayerType = request.EntityLayerType,
                OrderKey = FractionalIndex.Start() 
            };

            var lastFav = await db.Favorites
                .Where(f => f.WorkspaceMemberId == memberId)
                .OrderByDescending(f => f.OrderKey)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastFav != null)
            {
                newFav.OrderKey = FractionalIndex.SafeAfter(lastFav.OrderKey);
            }

            db.Favorites.Add(newFav);
            logger.LogInformation("Added favorite for EntityId {EntityId} by Member {MemberId}", request.EntityId, memberId);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
