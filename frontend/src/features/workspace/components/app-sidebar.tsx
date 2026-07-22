import { observer } from "mobx-react-lite";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { Inbox as InboxIcon, ListTodo, PanelLeftClose, PanelLeftOpen, Search, Settings, Users } from "lucide-react";
import { cn } from "@/lib/utils";
import { useWorkspace } from "../context/workspace-context";
import { useStore } from "@/stores/root.store";
import { WorkspaceSwitcher } from "./workspace-switcher";
import { GlobalSearch } from "./global-search";
import { ProjectNodeList } from "../contents/hierarchy/items/project-node-list";
import { FavoritesProjectsNav } from "./favorites-projects-nav";
import { UserMenu } from "./user-menu";

interface AppSidebarProps {
  onOpenProfile: () => void;
  collapsed?: boolean;
  onExpand?: () => void;
  onCollapse?: () => void;
}

export const AppSidebar = observer(function AppSidebar({ onOpenProfile, collapsed = false, onExpand, onCollapse }: Readonly<AppSidebarProps>) {
  const { workspaceId } = useWorkspace();
  const rootStore = useStore();
  const navigate = useNavigate();
  const pathname = useLocation({ select: (l) => l.pathname });
  const unreadCount = rootStore.notificationStore.unreadCount;

  const isInbox = pathname.endsWith("/inbox");
  const isMyTasks = pathname.endsWith("/my-tasks");
  const isMembers = pathname.endsWith("/members");

  if (collapsed) {
    return (
      <div className="h-full w-full flex flex-col items-start overflow-hidden">
        <div className="flex flex-col items-start gap-0.5 pt-2 pb-2 px-1.5 shrink-0 w-full border-b border-border/30">
          <button
            type="button"
            onClick={onExpand}
            title="Expand sidebar"
            className="h-7 w-7 flex items-center justify-center rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors cursor-pointer"
          >
            <PanelLeftOpen className="h-3.5 w-3.5" />
          </button>
          <WorkspaceSwitcher collapsed />
          <button
            type="button"
            onClick={onExpand}
            title="Search"
            className="h-7 w-7 flex items-center justify-center rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors cursor-pointer"
          >
            <Search className="h-3.5 w-3.5" />
          </button>
        </div>

        <div className="flex flex-col items-start gap-0.5 py-2 px-1.5 shrink-0 w-full border-b border-border/30">
          <button
            type="button"
            onClick={() => navigate({ to: "/workspaces/$workspaceId/inbox", params: { workspaceId } })}
            title="Inbox"
            className={cn(
              "relative h-7 w-7 flex items-center justify-center rounded-md transition-colors cursor-pointer",
              isInbox ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
            )}
          >
            <InboxIcon className="h-3.5 w-3.5" />
            {unreadCount > 0 && (
              <span className="absolute top-0.5 right-0.5 h-1.5 w-1.5 rounded-full bg-primary" />
            )}
          </button>
          <button
            type="button"
            onClick={() => navigate({ to: "/workspaces/$workspaceId/my-tasks", params: { workspaceId } })}
            title="My Tasks"
            className={cn(
              "h-7 w-7 flex items-center justify-center rounded-md transition-colors cursor-pointer",
              isMyTasks ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
            )}
          >
            <ListTodo className="h-3.5 w-3.5" />
          </button>
          <button
            type="button"
            onClick={() => navigate({ to: "/workspaces/$workspaceId/members", params: { workspaceId } })}
            title="Members"
            className={cn(
              "h-7 w-7 flex items-center justify-center rounded-md transition-colors cursor-pointer",
              isMembers ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
            )}
          >
            <Users className="h-3.5 w-3.5" />
          </button>
        </div>

        <div className="flex-1 min-h-0 w-full overflow-y-auto py-2 px-1.5 [&::-webkit-scrollbar]:w-0">
          <ProjectNodeList collapsed />
        </div>

        <div className="flex flex-col items-start gap-0.5 py-2 px-1.5 shrink-0 w-full border-t border-border/30">
          <button
            type="button"
            onClick={() => navigate({ to: "/workspaces/$workspaceId/settings", params: { workspaceId } })}
            title="Workspace settings"
            className="h-7 w-7 flex items-center justify-center rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors cursor-pointer"
          >
            <Settings className="h-3.5 w-3.5" />
          </button>
          <UserMenu onOpenProfile={onOpenProfile} />
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full flex flex-col overflow-hidden">
      <div className="flex items-center justify-between px-1.5 h-10 shrink-0 border-b border-border/40">
        <WorkspaceSwitcher />
        {onCollapse && (
          <button
            type="button"
            onClick={onCollapse}
            title="Collapse sidebar"
            className="h-6 w-6 flex items-center justify-center rounded-md text-muted-foreground hover:text-foreground hover:bg-muted/60 transition-colors cursor-pointer shrink-0"
          >
            <PanelLeftClose className="h-3.5 w-3.5" />
          </button>
        )}
      </div>

      <div className="px-2 pt-2 shrink-0">
        <GlobalSearch />
      </div>

      <nav className="px-2 pt-2 flex flex-col gap-0.5 shrink-0">
        <button
          type="button"
          onClick={() => navigate({ to: "/workspaces/$workspaceId/inbox", params: { workspaceId } })}
          className={cn(
            "flex items-center gap-2 h-7 px-2 rounded-md transition-colors cursor-pointer",
            isInbox ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
          )}
        >
          <InboxIcon className="h-3.5 w-3.5" />
          <span className="text-[11px] font-semibold">Inbox</span>
          {unreadCount > 0 && (
            <span className="ml-auto h-4 min-w-4 px-1 rounded-full bg-primary/15 text-primary text-[9px] font-bold flex items-center justify-center">
              {unreadCount > 99 ? "99+" : unreadCount}
            </span>
          )}
        </button>
        <button
          type="button"
          onClick={() => navigate({ to: "/workspaces/$workspaceId/my-tasks", params: { workspaceId } })}
          className={cn(
            "flex items-center gap-2 h-7 px-2 rounded-md transition-colors cursor-pointer",
            isMyTasks ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
          )}
        >
          <ListTodo className="h-3.5 w-3.5" />
          <span className="text-[11px] font-semibold">My Tasks</span>
        </button>
        <button
          type="button"
          onClick={() => navigate({ to: "/workspaces/$workspaceId/members", params: { workspaceId } })}
          className={cn(
            "flex items-center gap-2 h-7 px-2 rounded-md transition-colors cursor-pointer",
            isMembers ? "bg-primary/10 text-primary" : "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
          )}
        >
          <Users className="h-3.5 w-3.5" />
          <span className="text-[11px] font-semibold">Members</span>
        </button>
      </nav>

      <div className="flex-1 min-h-0 flex flex-col mt-2">
        <FavoritesProjectsNav />
      </div>

      <div className="flex items-center justify-between gap-1 px-2 py-2 shrink-0 border-t border-border/40">
        <button
          type="button"
          onClick={() => navigate({ to: "/workspaces/$workspaceId/settings", params: { workspaceId } })}
          className={cn(
            "flex items-center gap-2 h-7 px-2 rounded-md transition-colors cursor-pointer flex-1 min-w-0",
            "text-muted-foreground hover:bg-muted/50 hover:text-foreground",
          )}
        >
          <Settings className="h-3.5 w-3.5 shrink-0" />
          <span className="text-[11px] font-semibold">Settings</span>
        </button>
        <UserMenu onOpenProfile={onOpenProfile} />
      </div>
    </div>
  );
});
