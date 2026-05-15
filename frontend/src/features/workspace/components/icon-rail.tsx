import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
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
import { useWorkspaceSession } from "../context/workspace-provider";
import type { ContentPage } from "../type";

const NAV_ICONS: { id: ContentPage; icon: React.ElementType; label: string; hasSidebar?: boolean }[] =
  [
    { id: "projects", icon: Folder, label: "Projects", hasSidebar: true },
    { id: "members", icon: Users, label: "Members", hasSidebar: false },
    { id: "inbox", icon: Inbox, label: "Inbox", hasSidebar: false },
  ];

interface IconRailProps {
  onSelectIcon: (icon: ContentPage) => void;
  onCommandCenter: () => void;
}

export function IconRail({ onSelectIcon, onCommandCenter }: IconRailProps) {
  const navigate = useNavigate();
  const { state, actions } = useWorkspaceSession();

  return (
    <TooltipProvider delayDuration={100}>
      <div className="flex flex-col gap-0.5 h-full w-fit flex-shrink-0">
        {/* Top Card */}
        <div className="flex flex-col items-center gap-0.5 shrink-0 bg-background border border-border rounded-md shadow-sm p-1">
          {/* Expand — only when inner sidebar is closed */}
          {!state.isInnerSidebarOpen && (
            <>
              <Tooltip>
                <TooltipTrigger asChild>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="w-7 h-7 rounded-md text-[var(--theme-text-normal)] hover:text-[var(--theme-text-hover)] hover:bg-[var(--theme-item-hover)] transition-all duration-200"
                    onClick={actions.toggleInnerSidebar}
                  >
                    <PanelLeftOpen className="h-[18px] w-[18px]" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent
                  side="right"
                  sideOffset={10}
                  className="font-mono text-[10px] uppercase tracking-wider font-bold"
                >
                  Expand
                </TooltipContent>
              </Tooltip>
              <div className="w-4 h-px bg-border/60 my-0.5" />
            </>
          )}

          {/* Command Center */}
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant={
                  state.activeIcon === "command-center" ? "default" : "ghost"
                }
                size="icon"
                className={cn(
                  "w-7 h-7 rounded-md transition-all duration-200",
                  state.activeIcon === "command-center"
                    ? "theme-selected scale-105"
                    : "text-[var(--theme-text-normal)] hover:bg-[var(--theme-item-hover)] hover:text-[var(--theme-text-hover)]",
                )}
                onClick={onCommandCenter}
              >
                <LayoutGrid className="h-[18px] w-[18px]" />
              </Button>
            </TooltipTrigger>
            <TooltipContent
              side="right"
              sideOffset={10}
              className="font-mono text-[10px] uppercase tracking-wider font-bold"
            >
              Command Center
            </TooltipContent>
          </Tooltip>

          {/* Projects, Members */}
          {NAV_ICONS.map((item) => {
            // Projects icon should be active for projects, spaces, folders, and tasks
            const isActive =
              item.id === "projects"
                ? ["projects", "spaces", "folders", "tasks"].includes(
                    state.activeIcon || "",
                  )
                : state.activeIcon === item.id;

            const Icon =
              item.id === "projects" && isActive ? FolderOpen : item.icon;

            return (
              <Tooltip key={item.id}>
                <TooltipTrigger asChild>
                  <Button
                    variant={isActive ? "default" : "ghost"}
                    size="icon"
                    className={cn(
                      "w-7 h-7 rounded-md transition-all duration-300",
                      isActive
                        ? "bg-primary/10 text-primary scale-110 shadow-sm shadow-primary/5"
                        : "text-[var(--theme-text-normal)] hover:bg-[var(--theme-item-hover)] hover:text-[var(--theme-text-hover)]",
                    )}
                    onClick={() => onSelectIcon(item.id)}
                    onMouseEnter={() => {
                      if (!state.isInnerSidebarOpen && item.hasSidebar) {
                        actions.setHoveredIcon(item.id);
                      }
                    }}
                  >
                    <Icon
                      className={cn(
                        "h-[18px] w-[18px]",
                        isActive && "stroke-[2.5px]",
                      )}
                    />
                  </Button>
                </TooltipTrigger>
                <TooltipContent
                  side="right"
                  sideOffset={10}
                  className="font-mono text-[10px] uppercase tracking-wider font-bold"
                >
                  {item.label}
                </TooltipContent>
              </Tooltip>
            );
          })}

          {/* Settings removed from here */}
        </div>

        {/* Spacer */}
        <div className="flex-1" />

        {/* Bottom Card — Settings & Exit */}
        <div className="flex flex-col items-center gap-0.5 bg-background border border-border rounded-md shadow-sm p-1">
          {/* Settings */}
          <Tooltip>
            <TooltipTrigger asChild>
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
              >
                <Settings className="h-4 w-4" />
              </Button>
            </TooltipTrigger>
            <TooltipContent
              side="right"
              sideOffset={10}
              className="font-mono text-[10px] uppercase tracking-wider font-bold"
            >
              Settings
            </TooltipContent>
          </Tooltip>

          {/* Exit */}
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="w-7 h-7 rounded-md hover:bg-destructive/10 hover:text-destructive text-[var(--theme-text-normal)] transition-colors opacity-40 hover:opacity-100"
                onClick={() => navigate({ to: "/" })}
              >
                <LogOut className="h-4 w-4" />
              </Button>
            </TooltipTrigger>
            <TooltipContent
              side="right"
              sideOffset={10}
              className="font-mono text-[10px] uppercase tracking-wider font-bold text-destructive"
            >
              Exit
            </TooltipContent>
          </Tooltip>
        </div>
      </div>
    </TooltipProvider>
  );
}
