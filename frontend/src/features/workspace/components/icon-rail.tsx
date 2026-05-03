import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import {
  Command,
  CheckSquare,
  Users,
  Settings,
  PanelLeftOpen,
  Briefcase,
  LogOut,
} from "lucide-react";
import { useNavigate } from "@tanstack/react-router";
import { useAuth } from "@/features/auth/auth-context";
import { useWorkspaceSession } from "../context/workspace-provider";
import type { ContentPage } from "../type";

const NAV_ICONS: { id: ContentPage; icon: React.ElementType; label: string }[] =
  [
    { id: "projects", icon: CheckSquare, label: "Projects" },
    { id: "members", icon: Users, label: "Members" },
  ];

interface IconRailProps {
  onSelectIcon: (icon: ContentPage) => void;
  onCommandCenter: () => void;
}

export function IconRail({ onSelectIcon, onCommandCenter }: IconRailProps) {
  const navigate = useNavigate();
  const { user } = useAuth();
  const { state, actions } = useWorkspaceSession();

  return (
    <TooltipProvider delayDuration={100}>
      <div className="flex flex-col gap-2 h-full w-fit flex-shrink-0">
        {/* Top Card */}
        <div className="flex flex-col items-center gap-1.5 shrink-0 bg-background border border-border rounded-md shadow-sm p-1.5">
          {/* Expand — only when inner sidebar is closed */}
          {!state.isInnerSidebarOpen && (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon"
                  className="w-8 h-8 rounded-md text-[var(--theme-text-normal)] hover:text-[var(--theme-text-hover)] hover:bg-[var(--theme-item-hover)] transition-all duration-200"
                  onClick={actions.toggleInnerSidebar}
                >
                  <PanelLeftOpen className="h-5 w-5" />
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
          )}

          {/* Command Center */}
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant={state.activeIcon === "command-center" ? "default" : "ghost"}
                size="icon"
                className={cn(
                  "w-8 h-8 rounded-md transition-all duration-200",
                  state.activeIcon === "command-center"
                    ? "theme-selected scale-105"
                    : "text-[var(--theme-text-normal)] hover:bg-[var(--theme-item-hover)] hover:text-[var(--theme-text-hover)]",
                )}
                onClick={onCommandCenter}
              >
                <Command className="h-5 w-5" />
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
            const Icon = item.icon;
            
            // Projects icon should be active for projects, spaces, folders, and tasks
            const isActive = item.id === "projects" 
              ? ["projects", "spaces", "folders", "tasks"].includes(state.activeIcon || "")
              : state.activeIcon === item.id;

            return (
              <Tooltip key={item.id}>
                <TooltipTrigger asChild>
                  <Button
                    variant={isActive ? "default" : "ghost"}
                    size="icon"
                    className={cn(
                      "w-8 h-8 rounded-md transition-all duration-200",
                      isActive
                        ? "theme-selected scale-105"
                        : "text-[var(--theme-text-normal)] hover:bg-[var(--theme-item-hover)] hover:text-[var(--theme-text-hover)]",
                    )}
                    onClick={() => onSelectIcon(item.id)}
                    onMouseEnter={() => {
                      if (!state.isInnerSidebarOpen) {
                        actions.setHoveredIcon(item.id);
                      }
                    }}
                    onMouseLeave={() => actions.setHoveredIcon(null)}
                  >
                    <Icon className="h-5 w-5" />
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

          {/* Settings */}
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant={state.activeIcon === "settings" ? "default" : "ghost"}
                size="icon"
                className={cn(
                  "w-8 h-8 rounded-md transition-all duration-200",
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
        </div>

        {/* Spacer */}
        <div className="flex-1" />

        {/* Bottom Card — Workspaces, User, Exit */}
        <div className="flex flex-col items-center gap-2 bg-background border border-border rounded-md shadow-sm p-1.5">
          {/* Workspaces */}
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="w-8 h-8 rounded-md hover:bg-[var(--theme-item-hover)] hover:text-[var(--theme-text-hover)] text-[var(--theme-text-normal)] transition-all duration-200"
              >
                <Briefcase className="h-4 w-4" />
              </Button>
            </TooltipTrigger>
            <TooltipContent
              side="right"
              sideOffset={10}
              className="font-mono text-[10px] uppercase tracking-wider font-bold"
            >
              Workspaces
            </TooltipContent>
          </Tooltip>

          {/* User */}
          <Tooltip>
            <TooltipTrigger asChild>
              <Avatar className="h-8 w-8 border border-border shadow-sm hover:scale-110 transition-transform cursor-pointer rounded-md overflow-hidden bg-muted">
                <AvatarImage src="" />
                <AvatarFallback className="text-[10px] font-bold">
                  {user?.name?.substring(0, 2).toUpperCase() || (
                    <Users className="h-4 w-4" />
                  )}
                </AvatarFallback>
              </Avatar>
            </TooltipTrigger>
            <TooltipContent
              side="right"
              sideOffset={10}
              className="font-mono text-[10px] uppercase tracking-wider font-bold"
            >
              {user?.name || "Profile"}
            </TooltipContent>
          </Tooltip>

          {/* Exit */}
          <div className="pt-1">
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon"
                  className="w-8 h-8 rounded-md hover:bg-destructive/10 hover:text-destructive text-[var(--theme-text-normal)] transition-colors opacity-40 hover:opacity-100"
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
      </div>
    </TooltipProvider>
  );
}
