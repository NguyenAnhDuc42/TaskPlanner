"use client";

import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { ScrollArea } from "@/components/ui/scroll-area";
import { ChevronLeft, Home, Folder, FileText } from "lucide-react";
import { useSidebarContext } from "./sidebar-provider";
import { cn } from "@/lib/utils";
import type { ContentPage } from "../type";

interface ContentSidebarProps {
  isPopover?: boolean;
  contentPage?: ContentPage;
}

// Mock navigation items - these would be different for each content page
const getNavigationItems = (contentType: ContentPage) => {
  switch (contentType) {
    case "dashboard":
      return [
        { id: "overview", icon: Home, label: "Overview" },
        { id: "analytics", icon: FileText, label: "Analytics" },
        { id: "reports", icon: Folder, label: "Reports" },
      ];
    case "tasks":
      return [
        { id: "all-tasks", icon: FileText, label: "All Tasks" },
        { id: "my-tasks", icon: Home, label: "My Tasks" },
        { id: "completed", icon: Folder, label: "Completed" },
      ];
    case "members":
      return [
        { id: "all-members", icon: Home, label: "All Members" },
        { id: "teams", icon: Folder, label: "Teams" },
        { id: "roles", icon: FileText, label: "Roles" },
      ];
    case "calendar":
      return [
        { id: "month", icon: Home, label: "Month View" },
        { id: "week", icon: Folder, label: "Week View" },
        { id: "day", icon: FileText, label: "Day View" },
      ];
    case "settings":
      return [
        { id: "general", icon: Home, label: "General" },
        { id: "account", icon: Folder, label: "Account" },
        { id: "privacy", icon: FileText, label: "Privacy" },
      ];
    default:
      return [
        { id: "item-1", icon: Home, label: "Item 1" },
        { id: "item-2", icon: Folder, label: "Item 2" },
      ];
  }
};

export function ContentSidebar({
  isPopover = false,
  contentPage,
}: ContentSidebarProps) {
  const { activeContent, toggleInnerSidebar } = useSidebarContext();
  const displayContent = contentPage || activeContent;
  const navItems = getNavigationItems(displayContent);

  return (
    <div
      className={cn(
        "h-full flex flex-col bg-background",
        !isPopover && "border-r",
        isPopover && "rounded-xl border shadow-lg"
      )}
    >
      {/* Header with close button (only shown when not in popover) */}
      {!isPopover && (
        <>
          <div className="flex items-center justify-between p-4">
            <h2 className="font-semibold capitalize">{displayContent}</h2>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              onClick={toggleInnerSidebar}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
          </div>
          <Separator />
        </>
      )}

      <ScrollArea className="flex-1">
        <div className="p-3 space-y-1">
          {isPopover && (
            <div className="px-2 py-1.5 text-sm font-semibold capitalize mb-2">
              {displayContent}
            </div>
          )}
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <Button
                key={item.id}
                variant="ghost"
                className="w-full justify-start gap-3"
                onClick={() => {
                  console.log("Navigate to:", item.id);
                }}
              >
                <Icon className="h-4 w-4" />
                <span>{item.label}</span>
              </Button>
            );
          })}
        </div>
      </ScrollArea>

      {/* Footer info (optional for popover) */}
      {isPopover && (
        <>
          <Separator />
          <div className="p-3 text-xs text-muted-foreground">
            Click to navigate
          </div>
        </>
      )}
    </div>
  );
}
