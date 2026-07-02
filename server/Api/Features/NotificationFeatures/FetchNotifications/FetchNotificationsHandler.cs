using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Api;

public class FetchNotificationsHandler(
    TaskPlanDbContext db,
    CurrentUserService currentUserService,
    CursorHelper cursorHelper
) : IQueryHandler<FetchNotificationsQuery, FetchNotificationsResult>
{
    public async Task<Result<FetchNotificationsResult>> Handle(FetchNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.CurrentUserId();
        if (userId == Guid.Empty)
            return Result<FetchNotificationsResult>.Failure(UserError.NotFound);

        var conn = db.Database.GetDbConnection();

        var cursorData = request.Cursor != null ? cursorHelper.DecodeCursor(request.Cursor) : null;
        var cursorId = cursorData?.Values.GetValueOrDefault("Id")?.ToString();

        var rows = (await conn.QueryAsync<NotificationRecord>(@"
            SELECT
                n.id AS Id, n.type AS Type, n.entity_type AS EntityType, n.entity_id AS EntityId,
                n.project_workspace_id AS WorkspaceId, n.title AS Title, n.body AS Body,
                n.is_read AS IsRead, n.created_at AS CreatedAt,
                u.name AS ActorName
            FROM notifications n
            LEFT JOIN users u ON u.id = n.actor_user_id
            WHERE n.recipient_user_id = @UserId
              AND n.deleted_at IS NULL
              AND (@UnreadOnly = false OR n.is_read = false)
              AND (@CursorId::uuid IS NULL OR n.id < @CursorId::uuid)
            ORDER BY n.created_at DESC, n.id DESC
            LIMIT @Limit;",
            new
            {
                UserId = userId,
                UnreadOnly = request.UnreadOnly,
                CursorId = cursorId,
                Limit = request.Limit + 1,
            })).AsList();

        var hasMore = rows.Count > request.Limit;
        if (hasMore) rows.RemoveAt(rows.Count - 1);

        string? nextCursor = hasMore
            ? cursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object> { { "Id", rows[^1].Id.ToString() } }))
            : null;

        var unreadCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM notifications WHERE recipient_user_id = @UserId AND is_read = false AND deleted_at IS NULL",
            new { UserId = userId });

        return Result<FetchNotificationsResult>.Success(
            new FetchNotificationsResult(rows, nextCursor, hasMore, unreadCount));
    }
}
