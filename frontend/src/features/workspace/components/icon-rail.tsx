import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  LayoutGrid,
  Inbox,
  Folder,
  FolderOpen,
  Users,
  Settings,
  PanelLeftOpen,
  LogOut,
} from "lucide-react";
import { useNavigate } from "@tanstack/react-router";
import { useWorkspaceSession } from "../context/workspace-context";
import type { ContentPage } from "../type";

const NAV_ICONS: { 
  id: ContentPage; 
  icon: React.ElementType; 
  hasSidebar?: boolean;
  activeColorClass: string;
}[] = [
  { id: "projects", icon: Folder, hasSidebar: true, activeColorClass: "bg-chart-1/10 text-chart-1 shadow-sm shadow-chart-1/5" }, // Terracotta
  { id: "members", icon: Users, hasSidebar: false, activeColorClass: "bg-chart-2/10 text-chart-2 shadow-sm shadow-chart-2/5" }, // Denim
  { id: "inbox", icon: Inbox, hasSidebar: false, activeColorClass: "bg-chart-3/10 text-chart-3 shadow-sm shadow-chart-3/5" }, // Sage
];

interface IconRailProps {
  readonly onSelectIcon: (icon: ContentPage) => void;
  readonly onCommandCenter: () => void;
}

export function IconRail({ onSelectIcon, onCommandCenter }: Readonly<IconRailProps>) {
  const navigate = useNavigate();
  const { state, actions } = useWorkspaceSession();

  return (
    <div className="flex flex-col gap-0.5 h-full w-fit shrink-0">
      {/* Top Card */}
      <div className="flex flex-col items-center gap-0.5 shrink-0 bg-card border border-border rounded-md shadow-sm p-1">
        {/* Expand — only when inner sidebar is closed */}
        {!state.isInnerSidebarOpen && (
          <>
            <Button
              variant="ghost"
              size="icon"
              className="w-7 h-7 rounded-md text-(--theme-text-normal) hover:text-(--theme-text-hover) hover:bg-(--theme-item-hover) transition-all duration-200"
              onClick={actions.toggleInnerSidebar}
            >
              <PanelLeftOpen className="h-[18px] w-[18px]" />
            </Button>
            <div className="w-4 h-px bg-border/60 my-0.5" />
          </>
        )}

        {/* Command Center */}
        <Button
          variant={state.activeIcon === "command-center" ? "default" : "ghost"}
          size="icon"
          className={cn(
            "w-7 h-7 rounded-md transition-all duration-200",
            state.activeIcon === "command-center"
              ? "theme-selected scale-105"
              : "text-[var(--theme-text-normal)] hover:bg-[var(--theme-item-hover)] hover:text-[var(--theme-text-hover)]",
          )}
          onClick={onCommandCenter}
          onMouseEnter={() => actions.setHoveredIcon("command-center")}
        >
          <LayoutGrid className="h-[18px] w-[18px]" />
        </Button>

        {/* Projects, Members, Inbox */}
        {NAV_ICONS.map((item) => {
          const isActive =
            item.id === "projects"
              ? ["projects", "spaces", "folders", "tasks"].includes(state.activeIcon || "")
              : state.activeIcon === item.id;

          const Icon = item.id === "projects" && isActive ? FolderOpen : item.icon;

          return (
            <Button
              key={item.id}
              variant={isActive ? "default" : "ghost"}
              size="icon"
              className={cn(
                "w-7 h-7 rounded-md transition-all duration-300",
                isActive
                  ? item.activeColorClass + " scale-110"
                  : "text-[var(--theme-text-normal)] hover:bg-[var(--theme-item-hover)] hover:text-[var(--theme-text-hover)]",
              )}
              onClick={() => onSelectIcon(item.id)}
              onMouseEnter={() => {
                if (item.hasSidebar) {
                  actions.setHoveredIcon(item.id);
                } else {
                  actions.setHoveredIcon(null);
                }
              }}
            >
              <Icon className={cn("h-[18px] w-[18px]", isActive && "stroke-[2.5px]")} />
            </Button>
          );
        })}
      </div>

      {/* Spacer */}
      <div className="flex-1" />

      {/* Bottom Card — Settings & Exit */}
      <div className="flex flex-col items-center gap-0.5 bg-card border border-border rounded-md shadow-sm p-1">
        <Button
          variant={state.activeIcon === "settings" ? "default" : "ghost"}
          size="icon"
          className={cn(
            "w-7 h-7 rounded-md transition-all duration-200",
            state.activeIcon === "settings"
              ? "theme-selected scale-105"
              : "text-[var(--theme-text-normal)] hover:bg-[var(--theme-item-hover)] hover:text-[var(--theme-text-hover)]",
          )}
          onClick={() => onSelectIcon("settings")}
          onMouseEnter={() => actions.setHoveredIcon("settings")}
        >
          <Settings className="h-4 w-4" />
        </Button>

        <Button
          variant="ghost"
          size="icon"
          className="w-7 h-7 rounded-md hover:bg-destructive/10 hover:text-destructive text-[var(--theme-text-normal)] transition-colors opacity-40 hover:opacity-100"
          onClick={() => {
            localStorage.removeItem("lastWorkspaceId");
            navigate({ to: "/" });
          }}
          onMouseEnter={() => actions.setHoveredIcon(null)}
        >
          <LogOut className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}
