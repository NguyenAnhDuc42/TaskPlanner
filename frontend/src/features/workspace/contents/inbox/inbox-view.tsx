import { useState, useCallback, useRef, useEffect } from "react";
import { useSelector, useDispatch } from "react-redux";
import { useNavigate } from "@tanstack/react-router";
import { Bell, Check, Inbox, Loader2 } from "lucide-react";
import { UserAvatar } from "@/components/user-avatar";
import { cn } from "@/lib/utils";
import { formatDistanceToNow } from "date-fns";
import { notificationSelectors } from "@/store/entityStore";
import { useGetNotificationsQuery, useMarkNotificationsReadMutation, notificationsApi } from "@/features/notifications/notifications-api";
import type { AppDispatch } from "@/store";
import type { NotificationRecord } from "@/types/notification-record";

const TYPE_LABELS: Record<string, string> = {
  comment_added:  "💬 Comment",
  task_assigned:  "✅ Assigned",
  mention:        "@ Mention",
  status_changed: "🔄 Status",
  join_request:   "👋 Join Request",
};

function notificationPath(n: NotificationRecord): string | null {
  if (!n.workspaceId || !n.entityId) return null;
  if (n.entityType === "task")   return `/workspaces/${n.workspaceId}/tasks/${n.entityId}`;
  if (n.entityType === "space")  return `/workspaces/${n.workspaceId}/spaces/${n.entityId}`;
  if (n.entityType === "folder") return `/workspaces/${n.workspaceId}/folders/${n.entityId}`;
  return `/workspaces/${n.workspaceId}`;
}

type Filter = "all" | "unread";

export function InboxView() {
  const dispatch = useDispatch<AppDispatch>();
  const navigate = useNavigate();
  const [filter, setFilter] = useState<Filter>("all");
  const loadMoreRef = useRef<HTMLDivElement>(null);

  const { isLoading, data, isFetching } = useGetNotificationsQuery({ cursor: null });
  const [markRead] = useMarkNotificationsReadMutation();

  // Load more when bottom sentinel enters view
  useEffect(() => {
    const el = loadMoreRef.current;
    if (!el) return;
    const observer = new IntersectionObserver(([entry]) => {
      if (entry.isIntersecting && data?.hasNextPage && !isFetching && data.nextCursor) {
        dispatch(notificationsApi.endpoints.getNotifications.initiate(
          { cursor: data.nextCursor },
          { subscribe: false }
        ));
      }
    }, { threshold: 0.1 });
    observer.observe(el);
    return () => observer.unobserve(el);
  }, [data?.hasNextPage, data?.nextCursor, isFetching, dispatch]);

  const all = useSelector(notificationSelectors.selectAll)
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());

  const displayed = filter === "unread" ? all.filter(n => !n.isRead) : all;
  const unreadCount = all.filter(n => !n.isRead).length;

  const handleClick = useCallback((n: NotificationRecord) => {
    if (!n.isRead) markRead({ ids: [n.id] });
    const path = notificationPath(n);
    if (path) navigate({ to: path });
  }, [markRead, navigate]);

  const handleMarkAllRead = useCallback(() => {
    markRead({});
  }, [markRead]);

  return (
    <div className="h-full flex flex-col bg-card/40 overflow-hidden">
      {/* Header */}
      <div className="shrink-0 px-6 py-4 border-b border-border/30 flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <Inbox className="h-5 w-5 text-primary/70" />
          <h1 className="text-sm font-black tracking-tight text-foreground/90">Inbox</h1>
          {unreadCount > 0 && (
            <span className="h-5 min-w-5 px-1.5 flex items-center justify-center rounded-md bg-primary text-[10px] font-black text-primary-foreground">
              {unreadCount}
            </span>
          )}
        </div>

        <div className="flex items-center gap-2">
          {/* Filter tabs */}
          <div className="flex items-center gap-0.5 bg-muted/40 rounded-md p-0.5">
            {(["all", "unread"] as const).map(f => (
              <button
                key={f}
                type="button"
                onClick={() => setFilter(f)}
                className={cn(
                  "h-6 px-3 rounded-md text-[10px] font-bold uppercase tracking-wide transition-colors",
                  filter === f
                    ? "bg-background text-foreground shadow-sm"
                    : "text-muted-foreground hover:text-foreground"
                )}
              >
                {f}
              </button>
            ))}
          </div>

          {unreadCount > 0 && (
            <button
              type="button"
              onClick={handleMarkAllRead}
              className="flex items-center gap-1 h-7 px-2.5 rounded-md text-[10px] font-semibold text-muted-foreground hover:text-foreground hover:bg-muted/50 transition-colors border border-border/30"
            >
              <Check className="h-3 w-3" />
              Mark all read
            </button>
          )}
        </div>
      </div>

      {/* List */}
      <div className="flex-1 overflow-y-auto divide-y divide-border/15">
        {isLoading && all.length === 0 && (
          <div className="py-16 text-center text-sm text-muted-foreground/40 animate-pulse">Loading...</div>
        )}

        {!isLoading && displayed.length === 0 && (
          <div className="flex flex-col items-center justify-center py-24 gap-3">
            <Bell className="h-10 w-10 text-muted-foreground/15" />
            <p className="text-sm font-medium text-muted-foreground/40">
              {filter === "unread" ? "You're all caught up" : "No notifications yet"}
            </p>
            {filter === "unread" && (
              <button
                type="button"
                onClick={() => setFilter("all")}
                className="text-[11px] text-primary/60 hover:text-primary transition-colors"
              >
                View all notifications
              </button>
            )}
          </div>
        )}

        {displayed.map((n) => (
          <button
            key={n.id}
            type="button"
            onClick={() => handleClick(n)}
            className={cn(
              "w-full flex items-start gap-4 px-6 py-4 text-left transition-colors hover:bg-muted/30",
              !n.isRead && "bg-primary/3 border-l-2 border-l-primary/40"
            )}
          >
            {/* Actor avatar */}
            <UserAvatar
              name={n.actorName || "System"}
              className="h-8 w-8 shrink-0 mt-0.5 rounded-md"
              fallbackClassName="text-[10px] rounded-md"
            />

            <div className="flex-1 min-w-0">
              {/* Type badge + time */}
              <div className="flex items-center gap-2 mb-1">
                {n.type in TYPE_LABELS && (
                  <span className="text-[9px] font-black uppercase tracking-wider text-muted-foreground/50">
                    {TYPE_LABELS[n.type]}
                  </span>
                )}
                <span className="text-[9px] text-muted-foreground/35 font-mono ml-auto shrink-0">
                  {formatDistanceToNow(new Date(n.createdAt), { addSuffix: true })}
                </span>
              </div>

              {/* Title */}
              <p className={cn(
                "text-[12px] leading-snug",
                !n.isRead ? "text-foreground font-semibold" : "text-muted-foreground/70"
              )}>
                {n.title}
              </p>

              {/* Body snippet */}
              {n.body && (
                <p className="text-[11px] text-muted-foreground/50 mt-0.5 line-clamp-2 leading-relaxed">
                  {n.body}
                </p>
              )}
            </div>

            {/* Unread dot */}
            {!n.isRead && (
              <div className="h-2 w-2 rounded-full bg-primary shrink-0 mt-2" />
            )}
          </button>
        ))}

        {/* Intersection sentinel for auto load-more */}
        <div ref={loadMoreRef} className="h-4 flex items-center justify-center">
          {isFetching && (
            <Loader2 className="h-3.5 w-3.5 text-muted-foreground/30 animate-spin" />
          )}
        </div>
      </div>
    </div>
  );
}
