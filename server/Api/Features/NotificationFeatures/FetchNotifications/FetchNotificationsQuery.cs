namespace Api;

// Notification is a read-replica entity like Workspace — bypasses Bootstrap/Delta entirely,
// so a Get query is genuinely needed. NOT IAuthorizedWorkspaceRequest — notifications are
// scoped per-user across all their workspaces, not to a single workspace.
public record FetchNotificationsQuery(string? Cursor = null, int Limit = 20, bool UnreadOnly = false)
    : IQueryRequest<FetchNotificationsResult>;

public record FetchNotificationsResult(
    List<NotificationRecord> Items,
    string? NextCursor,
    bool HasNextPage,
    int UnreadCount
);
