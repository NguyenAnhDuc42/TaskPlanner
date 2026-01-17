"use client";

import { Button } from "@/components/ui/button";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
  ChevronRight,
  LayoutDashboard,
  CheckSquare,
  Calendar,
  Users,
  Settings,
  LogOut,
} from "lucide-react";
import { useNavigate } from "@tanstack/react-router";
import { useSidebarContext } from "./sidebar-provider";
import { type ContentPage } from "../type";
import { SidebarRegistry } from "./sidebar-registry";
import { Separator } from "@/components/ui/separator";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { useAuth } from "@/features/auth/auth-context";

interface NavItem {
  id: ContentPage;
  icon: React.ElementType;
  label: string;
}

const navItems: NavItem[] = [
  { id: "dashboard", icon: LayoutDashboard, label: "Dashboard" },
  { id: "tasks", icon: CheckSquare, label: "Tasks" },
  { id: "calendar", icon: Calendar, label: "Calendar" },
  { id: "members", icon: Users, label: "Members" },
  { id: "settings", icon: Settings, label: "Settings" },
];

export function OuterSidebar() {
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
      to: id === "dashboard" ? "/workspace/$workspaceId" : `/workspace/$workspaceId/${id}`,
      params: { workspaceId: workspaceId || "default" },
    });
  };

  return (
    <div className="relative h-full flex-shrink-0">
      {/* Visual Sidebar Shape */}
      <div className="h-full w-20 bg-background border rounded-xl shadow-lg flex flex-col items-center py-4 gap-4">
        <ScrollArea className="flex-1 w-full">
          <div className="flex flex-col items-center gap-6 px-2">
            {navItems.map((item) => {
              const Icon = item.icon;
              const isActive = activeContent === item.id;

              return (
                <div key={item.id} className="flex flex-col items-center gap-1">
                  <Button
                    variant={isActive ? "default" : "ghost"}
                    size="icon"
                    className="w-12 h-12 rounded-xl"
                    onClick={() => handleNavClick(item.id)}
                    onMouseEnter={() => {
                      if (!isInnerSidebarOpen) {
                        setHoveredIcon(item.id);
                      }
                    }}
                  >
                    <Icon className="h-6 w-6" />
                  </Button>
                  <span
                    className={cn(
                      "text-[10px] font-medium uppercase tracking-tight",
                      isActive ? "text-foreground" : "text-muted-foreground"
                    )}
                  >
                    {item.label}
                  </span>
                </div>
              );
            })}
          </div>
        </ScrollArea>

        <Separator className="w-12" />

        {/* User profile and Exit */}
        <div className="flex flex-col items-center gap-4 pb-2">
          <div className="flex flex-col items-center gap-1">
            <Avatar className="h-10 w-10 border-2 border-background shadow-sm hover:scale-105 transition-transform cursor-pointer">
              <AvatarImage src="" />
              <AvatarFallback className="bg-primary/10 text-primary text-xs font-bold">
                {user?.name?.substring(0, 2).toUpperCase() || "TP"}
              </AvatarFallback>
            </Avatar>
            <span className="text-[10px] font-medium text-muted-foreground uppercase">
              Profile
            </span>
          </div>

          <div className="flex flex-col items-center gap-1">
            <Button
              variant="ghost"
              size="icon"
              className="w-10 h-10 rounded-lg hover:bg-destructive/10 hover:text-destructive group"
              onClick={() => navigate({ to: "/" })}
            >
              <LogOut className="h-5 w-5 transition-transform group-hover:-translate-x-0.5" />
            </Button>
            <span className="text-[10px] font-medium text-muted-foreground uppercase">
              Exit
            </span>
          </div>
        </div>
      </div>

      {/* THE HOVER FRAME: Directly managed here */}
      {hoveredIcon && !isInnerSidebarOpen && (
        <div
          className="absolute top-0 left-[calc(100%+8px)] h-full w-64 z-50 animate-in fade-in slide-in-from-left-1"
          onMouseEnter={() => setHoveredIcon(hoveredIcon)}
          onMouseLeave={() => setHoveredIcon(null)}
        >
          <div className="h-full w-full bg-background border rounded-xl shadow-lg flex flex-col overflow-hidden">
            {/* Hover Header */}
            <div className="px-4 py-4 flex-shrink-0">
              <h3 className="font-semibold capitalize text-lg tracking-tight">
                {hoveredIcon}
              </h3>
            </div>
            <Separator />

            {/* Scrollable List from Registry */}
            <ScrollArea className="flex-1 px-3 py-4">
              <SidebarRegistry page={hoveredIcon} />
            </ScrollArea>

            <Separator />
            <div className="p-3 bg-muted/20 text-[10px] text-muted-foreground uppercase font-bold tracking-widest text-center">
              Quick Preview
            </div>
          </div>
        </div>
      )}

      {/* ARROW BUTTON */}
      {!isInnerSidebarOpen && (
        <Button
          variant="outline"
          size="icon"
          className="absolute top-1/2 right-[1px] -translate-y-1/2 translate-x-1/2 w-4 h-[80px] rounded-sm shadow-md z-[60] hover:bg-accent transition-colors p-0 bg-background"
          onClick={toggleInnerSidebar}
        >
          <ChevronRight className="h-2 w-2" />
        </Button>
      )}
    </div>
  );
}

// Helper function if not globally available, though it should be from "@/lib/utils"
// I'll add a check or just use raw strings if needed, but cn is standard here.
import { cn } from "@/lib/utils";
