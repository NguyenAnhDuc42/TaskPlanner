using Microsoft.EntityFrameworkCore;

namespace Application;

public class FavoriteTaskHandler(
    TaskPlanDbContext db, 
    WorkspaceContext workspaceContext) : ICommandHandler<FavoriteTaskCommand, TaskRecord>
{
    public async Task<Result<TaskRecord>> Handle(FavoriteTaskCommand command, CancellationToken cancellationToken)
    {
        var task =  await db.ProjectTasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == command.TaskId && t.DeletedAt == null);
        if (task is null) return Result<TaskRecord>.Failure(TaskError.NotFound);
        var favorite = new Favorite
        {
            WorkspaceMemberId = workspaceContext.CurrentMember.Id,
            EntityId = command.TaskId,
            EntityLayerType = EntityLayerType.ProjectTask
        };
        

        db.Favorites.Add(favorite);
        var affetedRows = await db.SaveChangesAsync(cancellationToken);
        if (affetedRows > 0)
        {
            return Result<TaskRecord>.Success(new TaskRecord{Id = command.TaskId,IsFavorite = true});
        }

        return Result<TaskRecord>.Failure(TaskError.NotFound);
    }
}