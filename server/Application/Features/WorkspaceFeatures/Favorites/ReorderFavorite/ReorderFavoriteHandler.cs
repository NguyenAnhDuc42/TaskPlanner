using Microsoft.EntityFrameworkCore;

namespace Application;

public class ReorderFavoriteHandler(TaskPlanDbContext db, WorkspaceContext context)
    : ICommandHandler<ReorderFavoriteCommand>
{
    public async Task<Result> Handle(ReorderFavoriteCommand request, CancellationToken cancellationToken)
    {
        var favorite = await db.Favorites
            .FirstOrDefaultAsync(f =>
                f.EntityId == request.EntityId
                && f.EntityLayerType == request.EntityLayerType
                && f.WorkspaceMemberId == context.CurrentMember.Id
                && f.DeletedAt == null, cancellationToken);

        if (favorite == null)
            return Result.Failure(Error.NotFound("Favorite.NotFound", "Favorite not found."));

        var newKey = ResolveOrderKey(request.PreviousOrderKey, request.NextOrderKey);
        if (newKey != null) favorite.OrderKey = newKey;

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string? ResolveOrderKey(string? prev, string? next)
    {
        if (prev == null && next == null) return null;
        if (prev != null && next != null)
        {
            return string.Compare(prev, next, StringComparison.Ordinal) >= 0
                ? FractionalIndex.After(prev)
                : FractionalIndex.Between(prev, next);
        }
        if (prev != null) return FractionalIndex.After(prev);
        return FractionalIndex.Before(next!);
    }
}
