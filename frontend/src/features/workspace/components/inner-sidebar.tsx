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

  const displayTitle = (
    activeContent === "projects" ? "Projects" :
    activeContent === "communications" ? "Chat" :
    activeContent
  );

  return (
    <div
      className={cn(
        "transition-all duration-300 ease-in-out glass-panel rounded-md flex flex-col h-full flex-shrink-0 relative overflow-hidden",
        isInnerSidebarOpen ? "w-64 opacity-100" : "w-0 opacity-0 pointer-events-none border-transparent",
        className
      )}
    >
      <div className={cn("w-full h-full flex flex-col", !isInnerSidebarOpen && "invisible")}>
        <div className="h-full flex flex-col overflow-hidden">
          {/* Header */}
          <div className="flex items-center justify-between p-4 flex-shrink-0 bg-transparent border-b border-border/10">
            <h2 className="font-bold text-[10px] uppercase tracking-[0.2em] text-[var(--theme-text-normal)]">
              {displayTitle}
            </h2>
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7 text-[var(--theme-text-normal)] hover:text-[var(--theme-text-hover)] hover:bg-[var(--theme-item-hover)] rounded-md transition-colors"
              onClick={toggleInnerSidebar}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
          </div>

          {/* Scrollable List from Registry */}
          <ScrollArea className="flex-1 px-3 py-4">
            <SidebarRegistry page={activeContent} />
          </ScrollArea>
          
          <div className="p-3 bg-transparent border-t border-border/10 text-[9px] text-[var(--theme-text-normal)] uppercase font-bold tracking-[0.2em] text-center opacity-60">
            Contextual Ops
          </div>
        </div>
      </div>
    </div>
  );
}
