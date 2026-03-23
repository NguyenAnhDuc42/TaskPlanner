import { useSidebarContext } from "./sidebar-provider";
import { type ContentPage } from "../type";
import { cn } from "@/lib/utils";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Button } from "@/components/ui/button";
import { ChevronLeft } from "lucide-react";
import { SidebarRegistry } from "./sidebar-registry";
import { useLocation } from "@tanstack/react-router";

export function InnerSidebar() {
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
        "transition-all duration-300 ease-in-out overflow-hidden flex flex-col h-full flex-shrink-0 bg-transparent",
        isInnerSidebarOpen ? "w-64 shadow-sm" : "w-0 shadow-none",
      )}
    >
      <div className="w-full h-full flex flex-col">
        <div className="bg-card border border-border/50 rounded-md shadow-sm h-full flex flex-col overflow-hidden">
          {/* Header */}
          <div className="flex items-center justify-between p-4 flex-shrink-0 bg-muted/10">
            <h2 className="font-bold text-xs uppercase tracking-widest text-foreground/60">
              {displayTitle}
            </h2>
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7 text-muted-foreground hover:text-foreground rounded-md transition-colors"
              onClick={toggleInnerSidebar}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
          </div>

          {/* Scrollable List from Registry */}
          <ScrollArea className="flex-1 px-3 py-4">
            <SidebarRegistry page={activeContent} />
          </ScrollArea>
          
          <div className="p-3 bg-muted/5 border-t border-border/50 text-[10px] text-muted-foreground uppercase font-bold tracking-widest text-center opacity-50">
            Contextual Ops
          </div>
        </div>
      </div>
    </div>
  );
}
