import { useState, useCallback } from "react";
import { Bell } from "lucide-react";
import { useSelector } from "react-redux";
import { useNavigate } from "@tanstack/react-router";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { notificationSelectors } from "@/store/entityStore";
import { useGetNotificationsQuery, useMarkNotificationsReadMutation } from "./notifications-api";
import { cn } from "@/lib/utils";
import { formatDistanceToNow } from "date-fns";
import type { NotificationRecord } from "@/types/notification-record";

function notificationPath(n: NotificationRecord): string | null {
  if (!n.workspaceId || !n.entityId) return null;
  if (n.entityType === "task")   return `/workspaces/${n.workspaceId}/tasks/${n.entityId}`;
  if (n.entityType === "space")  return `/workspaces/${n.workspaceId}/spaces/${n.entityId}`;
  if (n.entityType === "folder") return `/workspaces/${n.workspaceId}/folders/${n.entityId}`;
  return `/workspaces/${n.workspaceId}`;
}

export function NotificationBell() {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);

  // Load first page on mount; subsequent pages can be loaded on scroll
  const { data, isLoading } = useGetNotificationsQuery({ cursor: null });
  const [markRead] = useMarkNotificationsReadMutation();

  const notifications = useSelector(notificationSelectors.selectAll)
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());

  const unreadCount = notifications.filter(n => !n.isRead).length;
  // Take the max so the count reflects both API-loaded and real-time SignalR arrivals
  const displayCount = Math.max(data?.unreadCount ?? 0, unreadCount);

  const handleOpen = (isOpen: boolean) => {
    setOpen(isOpen);
  };

  const handleMarkAllRead = useCallback((e: React.MouseEvent) => {
    e.stopPropagation();
    markRead({});
  }, [markRead]);

  const handleClick = useCallback((n: NotificationRecord) => {
    if (!n.isRead) markRead({ ids: [n.id] });
    const path = notificationPath(n);
    if (path) navigate({ to: path });
    setOpen(false);
  }, [markRead, navigate]);

  return (
    <Popover open={open} onOpenChange={handleOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          className="relative h-7 w-7 flex items-center justify-center rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/50 transition-colors"
        >
          <Bell className="h-4 w-4" />
          {displayCount > 0 && (
            <span className="absolute -top-0.5 -right-0.5 h-4 min-w-4 px-0.5 flex items-center justify-center rounded-full bg-primary text-[9px] font-black text-primary-foreground leading-none">
              {displayCount > 99 ? "99+" : displayCount}
            </span>
          )}
        </button>
      </PopoverTrigger>

      <PopoverContent align="end" sideOffset={6} className="w-80 p-0 gap-0 border border-border shadow-md bg-background rounded-md overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between px-3 py-2 border-b border-border/50 bg-card/50">
          <span className="text-[11px] font-black uppercase tracking-wider text-foreground/70">Notifications</span>
          {displayCount > 0 && (
            <button
              onClick={handleMarkAllRead}
              className="text-[10px] text-primary/70 hover:text-primary font-semibold transition-colors cursor-pointer"
            >
              Mark all read
            </button>
          )}
        </div>

        {/* List */}
        <div className="max-h-[400px] overflow-y-auto divide-y divide-border/20">
          {isLoading && notifications.length === 0 && (
            <div className="py-8 text-center text-[11px] text-muted-foreground/40 animate-pulse">Loading...</div>
          )}
          {!isLoading && notifications.length === 0 && (
            <div className="py-10 text-center">
              <Bell className="h-7 w-7 mx-auto text-muted-foreground/20 mb-2" />
              <p className="text-[11px] text-muted-foreground/40">No notifications yet</p>
            </div>
          )}
          {notifications.map((n) => (
            <button
              key={n.id}
              type="button"
              onClick={() => handleClick(n)}
              className={cn(
                "w-full flex items-start gap-2.5 px-3 py-2.5 text-left transition-colors hover:bg-muted/40",
                !n.isRead && "bg-primary/5"
              )}
            >
              {/* Actor avatar */}
              <Avatar className="h-6 w-6 shrink-0 mt-0.5">
                <AvatarFallback className="text-[9px] bg-primary/10 text-primary font-black">
                  {n.actorName ? n.actorName.slice(0, 2).toUpperCase() : "?"}
                </AvatarFallback>
              </Avatar>

              <div className="flex-1 min-w-0">
                <p className={cn("text-[11px] leading-snug", !n.isRead ? "text-foreground font-medium" : "text-muted-foreground/80")}>
                  {n.title}
                </p>
                {n.body && (
                  <p className="text-[10px] text-muted-foreground/50 truncate mt-0.5">{n.body}</p>
                )}
                <p className="text-[9px] text-muted-foreground/35 mt-1 font-mono">
                  {formatDistanceToNow(new Date(n.createdAt), { addSuffix: true })}
                </p>
              </div>

              {!n.isRead && (
                <div className="h-1.5 w-1.5 rounded-full bg-primary shrink-0 mt-1.5" />
              )}
            </button>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  );
}
