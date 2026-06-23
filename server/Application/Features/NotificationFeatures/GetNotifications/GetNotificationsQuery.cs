namespace Application;

public record GetNotificationsQuery(string? Cursor = null, int Limit = 20, bool UnreadOnly = false)
    : IQueryRequest<GetNotificationsResponse>;

public record GetNotificationsResponse(
    List<NotificationRecord> Items,
    string? NextCursor,
    bool HasNextPage,
    int UnreadCount
);
