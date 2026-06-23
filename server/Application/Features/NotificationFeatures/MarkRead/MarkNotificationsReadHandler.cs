using Dapper;
using Microsoft.EntityFrameworkCore;
namespace Application;

public class MarkNotificationsReadHandler(TaskPlanDbContext db, CurrentUserService currentUserService)
    : ICommandHandler<MarkNotificationsReadCommand>
{
    public async Task<Result> Handle(MarkNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.CurrentUserId();
        if (userId == Guid.Empty)
            return Result.Failure(UserError.NotFound);

        var conn = db.Database.GetDbConnection();

        if (request.Ids == null || request.Ids.Count == 0)
        {
            await conn.ExecuteAsync(
                "UPDATE notifications SET is_read = true WHERE recipient_user_id = @UserId AND is_read = false",
                new { UserId = userId });
        }
        else
        {
            await conn.ExecuteAsync(
                "UPDATE notifications SET is_read = true WHERE recipient_user_id = @UserId AND id = ANY(@Ids)",
                new { UserId = userId, Ids = request.Ids.ToArray() });
        }

        return Result.Success();
    }
}
