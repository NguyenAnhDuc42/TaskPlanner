import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
  CheckSquare,
  Users,
  Settings,
  LogOut,
  MessageSquare,
  Plus,
  PanelLeftOpen,
  Briefcase,
} from "lucide-react";
import { useNavigate } from "@tanstack/react-router";
import { useSidebarContext } from "./sidebar-provider";
import { type ContentPage } from "../type";
import { SidebarRegistry } from "./sidebar-registry";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { useAuth } from "@/features/auth/auth-context";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";

interface NavItem {
  id: ContentPage;
  icon: React.ElementType;
  label: string;
}

const navItems: NavItem[] = [
  { id: "projects", icon: CheckSquare, label: "Projects" },
  { id: "members", icon: Users, label: "Members" },
  { id: "communications", icon: MessageSquare, label: "Communication" },
];

export function OuterSidebar({ className }: { className?: string }) {
  const navigate = useNavigate();
  const { user } = useAuth();
  const {
    isInnerSidebarOpen,
    toggleInnerSidebar,
    activeContent,
    setActiveContent,
    hoveredIcon,
    setHoveredIcon,
    workspaceId,
  } = useSidebarContext();

  const handleNavClick = (id: ContentPage) => {
    setActiveContent(id);
    navigate({
      to: `/workspaces/$workspaceId/${id}`,
      params: { workspaceId: workspaceId || "default" },
    });
  };

  return (
    <TooltipProvider delayDuration={100}>
      <div className={cn("relative h-full w-fit flex-shrink-0 flex flex-col items-center gap-2", className)}>
        {/* Top Section Card */}
        <div className="flex flex-col items-center gap-1.5 shrink-0 bg-background border border-border rounded-md shadow-sm p-1.5">
          {/* Expand Button - Only visible when closed, integrated with Nav Card */}
          {!isInnerSidebarOpen && (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon"
                  className="w-8 h-8 rounded-md text-[var(--theme-text-normal)] hover:text-[var(--theme-text-hover)] hover:bg-[var(--theme-item-hover)] transition-all duration-200"
                  onClick={toggleInnerSidebar}
                >
                  <PanelLeftOpen className="h-5 w-5" />
                </Button>
              </TooltipTrigger>
              <TooltipContent side="right" sideOffset={10} className="font-mono text-[10px] uppercase tracking-wider font-bold">
                Expand
              </TooltipContent>
            </Tooltip>
          )}

          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = activeContent === item.id;

            return (
              <Tooltip key={item.id}>
                <TooltipTrigger asChild>
                  <Button
                    variant={isActive ? "default" : "ghost"}
                    size="icon"
                    className={cn(
                      "w-8 h-8 rounded-md transition-all duration-200",
                      isActive ? "theme-selected scale-105" : "text-[var(--theme-text-normal)] hover:bg-[var(--theme-item-hover)] hover:text-[var(--theme-text-hover)]"
                    )}
                    onClick={() => handleNavClick(item.id)}
                    onMouseEnter={() => {
                      if (!isInnerSidebarOpen) {
                        setHoveredIcon(item.id);
                      }
                    }}
                  >
                    <Icon className="h-5 w-5" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent side="right" sideOffset={10} className="font-mono text-[10px] uppercase tracking-wider font-bold">
                  {item.label}
                </TooltipContent>
              </Tooltip>
            );
          })}

          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="w-8 h-8 rounded-md hover:bg-[var(--theme-item-hover)] hover:text-[var(--theme-text-hover)] text-[var(--theme-text-normal)] transition-all duration-200"
              >
                <Plus className="h-5 w-5" />
              </Button>
            </TooltipTrigger>
            <TooltipContent side="right" sideOffset={10} className="font-mono text-[10px] uppercase tracking-wider font-bold">
              Quick Action
            </TooltipContent>
          </Tooltip>

          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant={activeContent === "settings" ? "default" : "ghost"}
                size="icon"
                className={cn(
                  "w-8 h-8 rounded-md transition-all duration-200",
                  activeContent === "settings" ? "theme-selected shadow-md" : "text-[var(--theme-text-normal)] hover:bg-[var(--theme-item-hover)] hover:text-[var(--theme-text-hover)]"
                )}
                onClick={() => handleNavClick("settings")}
              >
                <Settings className="h-4 w-4" />
              </Button>
            </TooltipTrigger>
            <TooltipContent side="right" sideOffset={10} className="font-mono text-[10px] uppercase tracking-wider font-bold">
              Settings
            </TooltipContent>
          </Tooltip>
        </div>

        {/* Spacer */}
        <div className="flex-1" />

        {/* Bottom Section Card */}
        <div className="flex flex-col items-center gap-2 bg-background border border-border rounded-md shadow-sm p-1.5">

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
            <TooltipContent side="right" sideOffset={10} className="font-mono text-[10px] uppercase tracking-wider font-bold">
              Workspaces
            </TooltipContent>
          </Tooltip>

          <Tooltip>
            <TooltipTrigger asChild>
              <Avatar className="h-8 w-8 border border-border shadow-sm hover:scale-110 transition-transform cursor-pointer rounded-md overflow-hidden bg-muted">
                <AvatarImage src="" />
                <AvatarFallback className="text-[10px] font-bold">
                  {user?.name?.substring(0, 2).toUpperCase() || <Users className="h-4 w-4" />}
                </AvatarFallback>
              </Avatar>
            </TooltipTrigger>
            <TooltipContent side="right" sideOffset={10} className="font-mono text-[10px] uppercase tracking-wider font-bold">
              Profile
            </TooltipContent>
          </Tooltip>

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
              <TooltipContent side="right" sideOffset={10} className="font-mono text-[10px] uppercase tracking-wider font-bold text-destructive">
                Exit
              </TooltipContent>
            </Tooltip>
          </div>
        </div>

        {/* THE HOVER FRAME */}
        {hoveredIcon && !isInnerSidebarOpen && (
          <div
            className="absolute top-0 left-[calc(100%+12px)] h-full w-64 z-50 animate-in fade-in slide-in-from-left-2 duration-200"
            onMouseEnter={() => setHoveredIcon(hoveredIcon)}
            onMouseLeave={() => setHoveredIcon(null)}
          >
            <div className="h-full w-full bg-background border border-border rounded-2xl shadow-xl flex flex-col overflow-hidden">
              <div className="px-6 py-4 flex-shrink-0 bg-transparent border-b border-border/10">
                <h3 className="font-bold text-sm uppercase tracking-wider text-[var(--theme-text-hover)]">
                  {hoveredIcon}
                </h3>
              </div>

              <ScrollArea className="flex-1 px-3 py-4">
                <SidebarRegistry page={hoveredIcon} />
              </ScrollArea>

              <div className="p-3 bg-muted/10 border-t border-border/50 text-[9px] text-muted-foreground uppercase font-bold tracking-widest text-center">
                Quick Explorer
              </div>
            </div>
          </div>
        )}
      </div>
    </TooltipProvider>
  );
}

