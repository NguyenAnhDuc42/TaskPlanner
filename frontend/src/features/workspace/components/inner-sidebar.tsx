import { useSidebarContext } from "./sidebar-provider";
import { type ContentPage } from "../type";
import { cn } from "@/lib/utils";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Button } from "@/components/ui/button";
import { ChevronLeft } from "lucide-react";
import { SidebarRegistry } from "./sidebar-registry";
import { useLocation } from "@tanstack/react-router";

export function InnerSidebar({ className }: { className?: string }) {
  const location = useLocation();
  const { isInnerSidebarOpen, toggleInnerSidebar } = useSidebarContext();

  const segments = location.pathname.split("/");
  const activeContent = (segments[3] || "dashboard") as ContentPage;

  const displayTitle =
    activeContent === "projects" ? "Projects" :
    activeContent === "communications" ? "Chat" :
    activeContent;

  return (
    <div
      className={cn(
        "transition-all duration-300 ease-in-out flex flex-col h-full flex-shrink-0 relative overflow-hidden",
        isInnerSidebarOpen ? "w-64 opacity-100" : "w-0 opacity-0 pointer-events-none",
        className
      )}
    >
      <div className="flex-1 min-h-0 bg-background border border-border rounded-2xl h-full flex flex-col shadow-sm overflow-hidden">

        {/* Header */}
        <div className="flex items-center justify-between px-2 py-2 flex-shrink-0 border-b border-border">
          <h2 className="font-black text-sm uppercase tracking-widest text-foreground">
            {displayTitle}
          </h2>
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7 text-muted-foreground hover:text-foreground hover:bg-muted rounded-md transition-colors"
            onClick={toggleInnerSidebar}
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
        </div>

        {/* Scrollable content */}
        <ScrollArea className="flex-1 min-h-0">
          {/* Content Area */}
          <div className="flex-1 p-1 min-h-0 flex flex-col overflow-hidden">
            <SidebarRegistry page={activeContent} />
          </div>
        </ScrollArea>

      </div>
    </div>
  );
}
