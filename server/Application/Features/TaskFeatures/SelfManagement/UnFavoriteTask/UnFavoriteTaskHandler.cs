using Microsoft.EntityFrameworkCore;

namespace Application;
public class UnFavoriteTaskHandler(
    TaskPlanDbContext db,
    WorkspaceContext workspaceContext
) : ICommandHandler<UnFavoriteTaskCommand>
{
    public async Task<Result> Handle(UnFavoriteTaskCommand command, CancellationToken cancellationToken)
    {
        var favorite = await db.Favorites.FirstOrDefaultAsync(f => f.Id == command.FavoriteId && f.DeletedAt == null);
        if (favorite is null) return Result.Failure(FavoriteError.NotFound);

        favorite.SoftDelete();

        var rowaffected = await db.SaveChangesAsync(cancellationToken);
        if (rowaffected > 0)
        {
            return Result.Success();
        }

        return Result.Failure(CommonError.DatabaseError);

    }
}