"use client";

import { Button } from "@/components/ui/button";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
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
    isHovering,
    setIsHovering,
  } = useSidebarContext();

  return (
    <div className="relative h-screen w-16 flex-shrink-0">
      {/* Outer Sidebar - Floating Design */}
      <div className="absolute left-2 top-2 bottom-2 w-14 bg-background border rounded-xl shadow-lg flex flex-col items-center py-4 gap-2">
        <ScrollArea className="flex-1 w-full">
          <div className="flex flex-col items-center gap-2 px-2">
            {navItems.map((item) => {
              const Icon = item.icon;
              const isActive = activeContent === item.id;

              return (
                <Popover
                  key={item.id}
                  open={
                    isHovering &&
                    !isInnerSidebarOpen &&
                    activeContent === item.id
                  }
                  onOpenChange={(open) => setIsHovering(open)}
                >
                  <PopoverTrigger asChild>
                    <Button
                      variant={isActive ? "default" : "ghost"}
                      size="icon"
                      className="w-10 h-10 rounded-lg"
                      onClick={() => setActiveContent(item.id)}
                      onMouseEnter={() => {
                        if (!isInnerSidebarOpen && activeContent === item.id) {
                          setIsHovering(true);
                        }
                      }}
                      onMouseLeave={() => {
                        // Delay to allow moving to popover
                        setTimeout(() => setIsHovering(false), 100);
                      }}
                    >
                      <Icon className="h-5 w-5" />
                      <span className="sr-only">{item.label}</span>
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent
                    side="right"
                    align="start"
                    className="p-0 w-64 ml-2"
                    onMouseEnter={() => setIsHovering(true)}
                    onMouseLeave={() => setIsHovering(false)}
                  >
                    <ContentSidebar isPopover />
                  </PopoverContent>
                </Popover>
              );
            })}
          </div>
        </ScrollArea>
      </div>

      {/* Toggle Arrow Button - "Stick Through Hotdog" */}
      {!isInnerSidebarOpen && (
        <Button
          variant="outline"
          size="icon"
          className="absolute top-1/2 right-0 -translate-y-1/2 translate-x-1/2 w-6 h-12 rounded-full shadow-md z-50 hover:scale-110 transition-transform"
          onClick={toggleInnerSidebar}
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
      )}
    </div>
  );
}
