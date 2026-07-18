import { cn } from "@/lib/utils";
import { Folder, FolderOpen, Users, Inbox, Settings } from "lucide-react";
import { useWorkspaceSession } from "../../context/workspace-context";
import type { ContentPage } from "../../type";

interface MobileTabBarProps {
  readonly onSelectIcon: (icon: ContentPage) => void;
  readonly onOpenDrawer: () => void;
}

const TABS: { id: ContentPage; icon: React.ElementType; label: string }[] = [
  { id: "projects", icon: Folder, label: "Projects" },
  { id: "members", icon: Users, label: "Members" },
  { id: "inbox", icon: Inbox, label: "Inbox" },
  { id: "settings", icon: Settings, label: "Settings" },
];

export function MobileTabBar({ onSelectIcon, onOpenDrawer }: MobileTabBarProps) {
  const { state } = useWorkspaceSession();

  return (
    <nav className="flex items-center justify-around h-14 w-full shrink-0 bg-card border border-border rounded-md shadow-sm">
      {TABS.map((tab) => {
        const isActive =
          tab.id === "projects"
            ? ["projects", "spaces", "folders", "tasks"].includes(state.activeIcon || "")
            : state.activeIcon === tab.id;

        const Icon = tab.id === "projects" && isActive ? FolderOpen : tab.icon;

        return (
          <button
            key={tab.id}
            type="button"
            onClick={() => (tab.id === "projects" ? onOpenDrawer() : onSelectIcon(tab.id))}
            className={cn(
              "flex flex-col items-center justify-center gap-0.5 flex-1 h-full transition-colors",
              isActive ? "text-primary" : "text-muted-foreground",
            )}
          >
            <Icon className={cn("h-5 w-5", isActive && "stroke-[2.5px]")} />
            <span className="text-[9px] font-semibold">{tab.label}</span>
          </button>
        );
      })}
    </nav>
  );
}
