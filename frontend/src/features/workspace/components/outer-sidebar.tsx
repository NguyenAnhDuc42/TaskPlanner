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
} from "lucide-react";
import { useNavigate } from "@tanstack/react-router";
import { useSidebarContext } from "./sidebar-provider";
import { type ContentPage, getNavigationItems } from "../type";
import { Separator } from "@/components/ui/separator";

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
      to: id === "dashboard" ? "/workspace/$id" : `/workspace/$id/${id}`,
      params: { id: workspaceId || "default" },
    });
  };

  return (
    <div className="relative h-full flex-shrink-0">
      {/* Visual Sidebar Shape */}
      <div className="h-full w-full bg-background border rounded-xl shadow-lg flex flex-col items-center py-4 gap-2">
        <ScrollArea className="flex-1 w-full text-center">
          <div className="flex flex-col items-center gap-2 px-2">
            {navItems.map((item) => {
              const Icon = item.icon;
              const isActive = activeContent === item.id;

              return (
                <Button
                  key={item.id}
                  variant={isActive ? "default" : "ghost"}
                  size="icon"
                  className="w-10 h-10 rounded-lg"
                  onClick={() => handleNavClick(item.id)}
                  onMouseEnter={() => {
                    if (!isInnerSidebarOpen) {
                      setHoveredIcon(item.id);
                    }
                  }}
                >
                  <Icon className="h-5 w-5" />
                  <span className="sr-only">{item.label}</span>
                </Button>
              );
            })}
          </div>
        </ScrollArea>
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

            {/* Scrollable List */}
            <ScrollArea className="flex-1 px-3 py-4">
              <div className="space-y-1">
                {getNavigationItems(hoveredIcon).map((item) => {
                  const Icon = item.icon;
                  return (
                    <Button
                      key={item.id}
                      variant="ghost"
                      className="w-full justify-start gap-3 px-3 py-2 h-10 transition-colors hover:bg-accent/50"
                    >
                      <Icon className="h-4 w-4" />
                      <span className="text-sm font-medium">{item.label}</span>
                    </Button>
                  );
                })}
              </div>
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
