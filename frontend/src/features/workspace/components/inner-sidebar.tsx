import { useSidebarContext } from "./sidebar-provider";
import { type ContentPage } from "../type";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { ChevronLeft } from "lucide-react";
import { SidebarRegistry } from "./sidebar-registry";
import { useLocation } from "@tanstack/react-router";

export function InnerSidebar({ className }: { className?: string }) {
  const location = useLocation();
  const { isInnerSidebarOpen, toggleInnerSidebar } = useSidebarContext();

  const segments = location.pathname.split("/");
  const activeContent = (segments[3] || "projects") as ContentPage;

  const displayTitle =
    activeContent === "projects"
      ? "Projects"
      : activeContent === "communications"
        ? "Chat"
        : activeContent;

  return (
    <div
      className={cn(
        "transition-all duration-300 ease-in-out flex flex-col h-full flex-shrink-0 relative overflow-hidden",
        isInnerSidebarOpen
          ? "w-64 opacity-100"
          : "w-0 opacity-0 pointer-events-none",
        className,
      )}
    >
      <div className="flex-1 min-h-0 bg-background border border-border rounded-md h-full flex flex-col shadow-sm overflow-hidden">
        {/* Header */}
        <div className="h-12 flex items-center justify-between px-4 flex-shrink-0 border-b border-border">
          <h2 className="font-black text-[11px] uppercase tracking-widest text-foreground">
            {displayTitle}
          </h2>
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
            onClick={toggleInnerSidebar}
          >
            <ChevronLeft className="h-3.5 w-3.5" />
          </Button>
        </div>

        {/* Content Area */}
        <div className="flex-1 p-1 min-h-0 flex flex-col overflow-hidden">
          <SidebarRegistry page={activeContent} />
        </div>
      </div>
    </div>
  );
}
