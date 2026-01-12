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
import { useSidebarContext } from "./sidebar-provider";
import type { ContentPage } from "../type";
import { ContentSidebar } from "./content-sidebar";

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
  const {
    isInnerSidebarOpen,
    toggleInnerSidebar,
    activeContent,
    setActiveContent,
    hoveredIcon,
    setHoveredIcon,
  } = useSidebarContext();

  return (
    // THE FRAME: A fixed width container that holds the sidebar and its associated overlays
    <div className="relative h-full flex-shrink-0">
      {/* 1. Visual Sidebar Shape */}
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
                  onClick={() => setActiveContent(item.id)}
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

      {/* 2. The HOVER SPOT: Aligned perfectly with the top/bottom of the sidebar frame */}
      {hoveredIcon && !isInnerSidebarOpen && (
        <div
          className="absolute top-0 left-[calc(100%+8px)] h-full w-64 z-50 transition-all animate-in fade-in slide-in-from-left-2 duration-200"
          onMouseEnter={() => setHoveredIcon(hoveredIcon)}
          onMouseLeave={() => setHoveredIcon(null)}
        >
          <div className="h-full w-full bg-background border rounded-xl shadow-lg overflow-hidden">
            <ContentSidebar isPopover contentPage={hoveredIcon} />
          </div>
        </div>
      )}

      {/* 3. The ARROW BUTTON: "Stick through hotdog" - perfectly centered on the border line */}
      {!isInnerSidebarOpen && (
        <Button
          variant="outline"
          size="icon"
          // Using right-[-1px] to put the center exactly on the 1px border line
          // w-4 is 16px, so translate-x-1/2 puts 8px inside and 8px outside
          className="absolute top-1/2 right-[1px] -translate-y-1/2 translate-x-1/2 w-5 h-20 rounded-sm shadow-md z-[60] hover:bg-accent transition-colors p-0 bg-background"
          onClick={toggleInnerSidebar}
        >
          <ChevronRight className="h-2 w-2" />
        </Button>
      )}
    </div>
  );
}
